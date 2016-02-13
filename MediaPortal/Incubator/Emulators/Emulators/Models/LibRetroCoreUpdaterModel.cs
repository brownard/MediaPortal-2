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

namespace Emulators.Models
{
  public class LibRetroCoreUpdaterModel : IWorkflowModel
  {
    public static readonly Guid MODEL_ID = new Guid("656E3AC1-0363-4DA9-A23F-F1422A9ADD74");
    public const string LABEL_CORE_NAME = "CoreName";
    public const string LABEL_CORE_SYSTEM = "CoreSystem";
    public const string KEY_CORE = "LibRetro: Core";
    public const string DIALOG_CORE_UPDATE_PROGRESS = "dialog_core_update_progress";
    
    protected AbstractProperty _progressLabelProperty = new WProperty(typeof(string), null);

    protected readonly object _updateSync = new object();
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

    protected void DownloadCoreAsync(CoreUrl core)
    {
      ServiceRegistration.Get<IThreadPool>().Add(() => DownloadCore(core));
    }

    protected void DownloadCore(CoreUrl core)
    {
      if (_downloadingUrls.Contains(core.Url))
        return;

      try
      {
        ProgressLabel = LocalizationHelper.Translate("[Emulators.CoreUpdater.Downloading]", core.Name);
        var sm = ServiceRegistration.Get<IScreenManager>();
        Guid? dialogId = sm.ShowDialog(DIALOG_CORE_UPDATE_PROGRESS);

        LibRetroSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
        _coreHandler.DownloadCore(core, settings.CoresDirectory);

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
      var w = ServiceRegistration.Get<IThreadPool>().Add(Update);
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

        LibRetroSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
        _infoHandler.Update(settings.InfoDirectory);
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
      _coreItems.Clear();
      foreach (CoreUrl core in _coreHandler.OnlineCores)
        _coreItems.Add(CreateListItem(core));
      _coreItems.FireChange();
    }

    protected ListItem CreateListItem(CoreUrl core)
    {
      ListItem item = new ListItem();
      item.SetLabel(LABEL_CORE_NAME, core.Name);
      item.AdditionalProperties[KEY_CORE] = core;
      item.Command = new MethodDelegateCommand(() => DownloadCore(core));

      string coreName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(core.Name));
      CoreInfo info;
      if (_infoHandler.CoreInfos.TryGetValue(coreName, out info))
      {
        item.SetLabel(Consts.KEY_NAME, info.DisplayName);
        item.SetLabel(LABEL_CORE_SYSTEM, info.SystemName);
      }
      else
      {
        item.SetLabel(Consts.KEY_NAME, coreName);
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
