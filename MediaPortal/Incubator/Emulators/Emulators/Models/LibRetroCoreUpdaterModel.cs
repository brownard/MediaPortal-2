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

namespace Emulators.Models
{
  public class LibRetroCoreUpdaterModel : IWorkflowModel
  {
    public static readonly Guid MODEL_ID = new Guid("656E3AC1-0363-4DA9-A23F-F1422A9ADD74");
    protected static readonly string CORE_DIRECTORY = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\LibRetro\Cores\");
    protected CoreHandler _coreHandler;

    public LibRetroCoreUpdaterModel()
    {
      _coreHandler = new CoreHandler(CORE_DIRECTORY);
    }

    protected void Update()
    {
      _coreHandler.Update();
      if (_coreHandler.OnlineCores.Count > 0)
        _coreHandler.DownloadCore(_coreHandler.OnlineCores[0]);
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
      Update();
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
