using Emulators.Common.Emulators;
using Emulators.Common.GoodMerge;
using Emulators.Emulator;
using Emulators.GoodMerge;
using Emulators.Models;
using Emulators.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Game
{
  public class GameLauncher : IGameLauncher, IDisposable
  {
    protected static Key _mappedKey = Key.Stop;

    protected readonly object _syncRoot = new object();
    protected MediaItem _mediaItem;
    protected ILocalFsResourceAccessor _resourceAccessor;
    protected EmulatorProcess _emulatorProcess;
    protected Guid _doConfigureDialogHandle = Guid.Empty;
    protected AsynchronousMessageQueue _messageQueue;
    protected SettingsChangeWatcher<EmulatorsSettings> _settingsChangeWatcher = new SettingsChangeWatcher<EmulatorsSettings>();

    public GameLauncher()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new [] { DialogManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == DialogManagerMessaging.CHANNEL)
      {
        DialogManagerMessaging.MessageType messageType = (DialogManagerMessaging.MessageType)message.MessageType;
        if (messageType == DialogManagerMessaging.MessageType.DialogClosed)
        {
          Guid dialogHandle = (Guid)message.MessageData[DialogManagerMessaging.DIALOG_HANDLE];
          bool doConfigure = false;
          lock (_syncRoot)
            if (_doConfigureDialogHandle == dialogHandle)
            {
              _doConfigureDialogHandle = Guid.Empty;
              DialogResult dialogResult = (DialogResult)message.MessageData[DialogManagerMessaging.DIALOG_RESULT];
              if (dialogResult == DialogResult.Yes)
                doConfigure = true;
            }
          if (doConfigure)
            DoConfigureNewEmulator();
        }
      }
    }

    public bool LaunchGame(MediaItem mediaItem)
    {
      mediaItem.Aspects[VideoAspect.Metadata.AspectId] = new MediaItemAspect(VideoAspect.Metadata);
      PlayItemsModel.CheckQueryPlayAction(mediaItem);
      return true;
      _mediaItem = mediaItem;
      EmulatorConfiguration configuration;
      if (!TryGetConfiguration(mediaItem, out configuration))
      {
        ShowNoConfigurationDialog();
        return false;
      }

      lock (_syncRoot)
      {
        Cleanup();
        if (!mediaItem.GetResourceLocator().TryCreateLocalFsAccessor(out _resourceAccessor))
          return false;
        ServiceRegistration.Get<ILogger>().Debug("GameLauncher: Created LocalFsAccessor: {0}, {1}", _resourceAccessor.CanonicalLocalResourcePath, _resourceAccessor.LocalFileSystemPath);

        IEnumerable<string> goodmergeItems;
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodmergeItems))
        {
          string selectedItem;
          MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, out selectedItem);
          LaunchGoodmergeGame(_resourceAccessor, goodmergeItems, selectedItem, configuration);
          return true;
        }
        return LaunchGame(_resourceAccessor.LocalFileSystemPath, configuration);
      }
    }

    protected void LaunchGoodmergeGame(ILocalFsResourceAccessor accessor, IEnumerable<string> goodmergeItems, string lastPlayedItem, EmulatorConfiguration configuration)
    {
      GoodMergeSelectModel.Instance().Extract(accessor, goodmergeItems, lastPlayedItem, e => OnExtractionCompleted(e, configuration));
    }

    protected void OnExtractionCompleted(ExtractionCompletedEventArgs e, EmulatorConfiguration configuration)
    {
      lock (_syncRoot)
      {
        Cleanup();
        if (e.Success)
        {
          UpdateMediaItem(_mediaItem, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, e.ExtractedItem);
          LaunchGame(e.ExtractedPath, configuration);
        }
        else
        {
          ShowErrorDialog("[Emulators.ExtractionError.Label]");
        }
      }
    }

    protected bool LaunchGame(string path, EmulatorConfiguration configuration)
    {
      _emulatorProcess = new EmulatorProcess(path, configuration, _mappedKey);
      _emulatorProcess.Exited += ProcessExited;
      bool result = TryStartProcess();
      if (result)
      {
        OnGameStarted();
      }
      else
      {
        Cleanup();
        ShowErrorDialog("[Emulators.LaunchError.Label]");
      }
      return result;
    }

    protected bool TryGetConfiguration(MediaItem mediaItem, out EmulatorConfiguration configuration)
    {
      configuration = null;
      string mimeType;
      if (mediaItem == null || !MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_MIME_TYPE, out mimeType))
        return false;
      return ServiceRegistration.Get<IEmulatorManager>().TryGetConfiguration(mimeType, out configuration);
    }

    protected bool TryStartProcess()
    {
      try
      {
        return _emulatorProcess.Start();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("GameLauncher: Error starting process", ex);
      }
      return false;
    }

    protected void ProcessExited(object sender, EventArgs e)
    {
      lock (_syncRoot)
        if (sender == _emulatorProcess)
          OnGameExited();
    }

    protected void OnGameStarted()
    {
      if (_settingsChangeWatcher.Settings.MinimiseOnGameStart)
        ServiceRegistration.Get<IScreenControl>().Minimize();
    }

    protected void OnGameExited()
    {
      Cleanup();
      if (_settingsChangeWatcher.Settings.MinimiseOnGameStart)
        //SkinContext.Form.BeginInvoke((System.Windows.Forms.MethodInvoker)(() => ServiceRegistration.Get<IScreenControl>().Restore()));
        ServiceRegistration.Get<IScreenControl>().Restore();
    }

    protected void UpdateMediaItem<T>(MediaItem mediaItem, MediaItemAspectMetadata.AttributeSpecification attribute, T value)
    {
      if (mediaItem == null)
        return;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;
      var rl = mediaItem.GetResourceLocator();
      Guid parentDirectoryId;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, out parentDirectoryId))
        return;
      MediaItemAspect.SetAttribute(mediaItem.Aspects, attribute, value);
      cd.AddOrUpdateMediaItem(parentDirectoryId, rl.NativeSystemId, rl.NativeResourcePath, mediaItem.Aspects.Values);
    }

    protected void ShowErrorDialog(string text)
    {
      ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.Dialog.Error.Header]", text, DialogType.OkDialog, false, DialogButtonType.Ok);
    }

    protected void ShowNoConfigurationDialog()
    {
      Guid doConfigureHandle = ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.ConfigureEmulatorNow.Header]", "[Emulators.ConfigureEmulatorNow.Label]", DialogType.YesNoDialog, false, DialogButtonType.Yes);
      lock(_syncRoot)
        _doConfigureDialogHandle = doConfigureHandle;
    }

    protected void DoConfigureNewEmulator()
    {
      ServiceRegistration.Get<IWorkflowManager>().NavigatePush(EmulatorConfigurationModel.STATE_OVERVIEW);
    }

    protected void Cleanup()
    {
      if (_emulatorProcess != null)
      {
        _emulatorProcess.Exited -= ProcessExited;
        _emulatorProcess.Dispose();
        _emulatorProcess = null;
      }
      if (_resourceAccessor != null)
      {
        _resourceAccessor.Dispose();
        _resourceAccessor = null;
      }
    }

    public void Dispose()
    {
      Cleanup();
    }
  }
}