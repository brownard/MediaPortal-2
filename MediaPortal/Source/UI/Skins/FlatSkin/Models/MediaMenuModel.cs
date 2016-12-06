using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatSkin.Models
{
  public class MediaMenuModel : MenuModel
  {
    public static readonly Guid MODEL_ID = new Guid("5B9E07AE-1ECA-4DD8-9B63-93CAD83A4EBE");

    public static readonly Guid FILTER_ACTION_ID = new Guid("B15EA757-905E-46FA-A71D-C132885C221E");
    public static readonly Guid VIEW_MODE_ACTION_ID = new Guid("F400FBEA-6E32-4735-BE52-15DFDBB64FCC");
    public static readonly Guid SORTING_ACTION_ID = new Guid("D205E9BC-A970-429D-88F8-BBB92CF3F19A");
    public static readonly Guid[] BASIC_ACTIONS = new[]
    {
      FILTER_ACTION_ID,
      VIEW_MODE_ACTION_ID,
      SORTING_ACTION_ID
    };
    
    protected ItemsList _basicItems = new ItemsList();
    protected bool _isDirty = true;

    public ItemsList BasicItems
    {
      get
      {
        InitActions();
        return _basicItems;
      }
    }

    protected void InitActions()
    {
      if (!_isDirty)
        return;
      _isDirty = false;

      _basicItems.Clear();
      var menuActions = ServiceRegistration.Get<IWorkflowManager>().MenuStateActions;
      foreach (var mediaAction in BASIC_ACTIONS)
      {
        WorkflowAction action;
        if (menuActions.TryGetValue(mediaAction, out action))
          _basicItems.Add(CreateActionItem(action));
      }
      _basicItems.FireChange();
    }

    protected ListItem CreateActionItem(WorkflowAction action)
    {
      ListItem item = new ListItem("Name", action.DisplayTitle)
      {
        Command = new MethodDelegateCommand(action.Execute),
      };
      item.AdditionalProperties[Consts.KEY_ITEM_ACTION] = action;
      item.SetLabel("Help", action.HelpText);
      return item;
    }
  }
}
