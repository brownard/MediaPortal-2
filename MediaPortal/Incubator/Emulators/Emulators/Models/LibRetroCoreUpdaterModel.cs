using Emulators.LibRetro.Cores;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using SharpRetro.Info;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Common.Threading;
using MediaPortal.Common.Commands;
using System.IO;
using MediaPortal.Common.Settings;
using Emulators.LibRetro.Settings;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Common.Localization;
using Emulators.Models.Navigation;

namespace Emulators.Models
{
  public class LibRetroCoreUpdaterModel : IWorkflowModel
  {
    public static readonly Guid MODEL_ID = new Guid("656E3AC1-0363-4DA9-A23F-F1422A9ADD74");
    public const string LABEL_CORE_NAME = "CoreName";
    public const string KEY_CORE_INFO = "LibRetro: CoreInfo";
    public const string KEY_CORE = "LibRetro: Core";
    public const string DIALOG_CORE_UPDATE_PROGRESS = "dialog_core_update_progress";
    
    protected AbstractProperty _progressLabelProperty = new WProperty(typeof(string), null);

    protected readonly object _updateSync = new object();
    protected string _coresDirectory;
    protected string _infoDirectory;
    protected CoreHandler _coreHandler;
    protected CoreInfoHandler _infoHandler;
    protected ItemsList _coreItems;
    protected SynchronizedCollection<string> _downloadingUrls;
    protected bool _isUpdating;

    public LibRetroCoreUpdaterModel()
    {
      _coreItems = new ItemsList();
      _downloadingUrls = new SynchronizedCollection<string>();      
      _coreHandler = new CoreHandler();
      _infoHandler = new CoreInfoHandler();
    }

    public ItemsList Items
    {
      get { return _coreItems; }
    }

    public AbstractProperty ProgressLabelProperty
    {
      get { return _progressLabelProperty; }
    }

    public string ProgressLabel
    {
      get { return (string)_progressLabelProperty.GetValue(); }
      set { _progressLabelProperty.SetValue(value); }
    }

    protected void DownloadCoreAsync(LibRetroCoreItem item, LocalCore core)
    {
      ServiceRegistration.Get<IThreadPool>().Add(() => DownloadCore(item, core));
    }

    protected void DownloadCore(LibRetroCoreItem item, LocalCore core)
    {      
      if (_downloadingUrls.Contains(core.Url))
        return;

      try
      {
        ProgressLabel = LocalizationHelper.Translate("[Emulators.CoreUpdater.Downloading]", core.CoreName);
        var sm = ServiceRegistration.Get<IScreenManager>();
        Guid? dialogId = sm.ShowDialog(DIALOG_CORE_UPDATE_PROGRESS);
        if (_coreHandler.DownloadCore(core, _coresDirectory))
          item.Downloaded = true;

        if (dialogId.HasValue)
          sm.CloseDialog(dialogId.Value);
      }
      finally
      {
        _downloadingUrls.Remove(core.Url);
      }
    }

    protected void UpdateAsync()
    {
      LibRetroSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
      _coresDirectory = settings.CoresDirectory;
      _infoDirectory = settings.InfoDirectory;
      ServiceRegistration.Get<IThreadPool>().Add(Update);
    }

    protected void Update()
    {
      lock (_updateSync)
      {
        if (_isUpdating)
          return;
        _isUpdating = true;
      }

      try
      {
        ProgressLabel = "[Emulators.CoreUpdater.UpdatingInfo]";
        var sm = ServiceRegistration.Get<IScreenManager>();
        Guid? dialogId = sm.ShowDialog(DIALOG_CORE_UPDATE_PROGRESS);

        _infoHandler.Update(_infoDirectory);
        ProgressLabel = "[Emulators.CoreUpdater.UpdatingCores]";
        _coreHandler.Update();
        RebuildItemsList();

        if (dialogId.HasValue)
          sm.CloseDialog(dialogId.Value);
      }
      finally
      {
        lock (_updateSync)
          _isUpdating = false;
      }
    }

    protected void RebuildItemsList()
    {
      string coresDirectory = _coresDirectory; 
      _coreItems.Clear();
      foreach (LocalCore core in _coreHandler.Cores)
        _coreItems.Add(CreateListItem(core, coresDirectory));
      _coreItems.FireChange();
    }

    protected ListItem CreateListItem(LocalCore core, string coresDirectory)
    {
      LibRetroCoreItem item = new LibRetroCoreItem();
      item.SetLabel(LABEL_CORE_NAME, core.CoreName);
      item.AdditionalProperties[KEY_CORE] = core;
      item.Downloaded = File.Exists(Path.Combine(coresDirectory, core.CoreName));
      item.Command = new MethodDelegateCommand(() => DownloadCoreAsync(item, core));

      string infoName = Path.GetFileNameWithoutExtension(core.CoreName);
      CoreInfo info;
      if (_infoHandler.CoreInfos.TryGetValue(infoName, out info))
      {
        item.SetLabel(Consts.KEY_NAME, info.DisplayName);
        item.AdditionalProperties[KEY_CORE_INFO] = info;
      }
      else
      {
        item.SetLabel(Consts.KEY_NAME, infoName);
      }
      return item;
    }

    #region IWorkflow
    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UpdateAsync();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
    #endregion
  }
}
