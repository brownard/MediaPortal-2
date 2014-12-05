#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  [ContentProperty("Actions")]
  public class EventTrigger : TriggerBase, IAddChild<TriggerAction>
  {
    #region Protected fields

    protected AbstractProperty _routedEventProperty;
    protected TriggerActionCollection _actions;

    protected UIElement _registeredUIElement = null;

    #endregion

    #region Ctor

    public EventTrigger()
    {
      Init();
    }

    void Init()
    {
      _routedEventProperty = new SProperty(typeof(string), string.Empty);
      _actions = new TriggerActionCollection();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      EventTrigger t = (EventTrigger) source;
      RoutedEvent = t.RoutedEvent;
      foreach (TriggerAction action in t._actions)
        _actions.Add(copyManager.GetCopy(action));
    }

    public override void Dispose()
    {
      foreach (TriggerAction triggerAction in _actions)
        triggerAction.Dispose();
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty RoutedEventProperty
    {
      get { return _routedEventProperty; }
    }

    public string RoutedEvent
    {
      get { return (string) _routedEventProperty.GetValue(); }
      set { _routedEventProperty.SetValue(value); }
    }

    public TriggerActionCollection Actions
    {
      get { return _actions; }
    }

    #endregion

    #region Protected members

    protected void AttachToUIElement()
    {
      if (_element == null)
        return;
      if (_registeredUIElement != null)
        _registeredUIElement.EventOccured -= OnUIEvent;
      _element.EventOccured += OnUIEvent;
      _registeredUIElement = _element;
    }

    protected void DetachFromUIElement()
    {
      if (_element == null)
        return;
      if (_registeredUIElement != null)
        _registeredUIElement.EventOccured -= OnUIEvent;
      _registeredUIElement = null;
    }

    void OnUIEvent(string eventName)
    {
      if (RoutedEvent == eventName)
        foreach (TriggerAction ta in _actions)
          ta.Execute(_element);
    }

    #endregion

    #region Base overrides

    public override void Setup(UIElement element)
    {
      DetachFromUIElement();
      base.Setup(element);
      AttachToUIElement();
    }

    public override void Reset()
    {
      base.Reset();
      DetachFromUIElement();
    }

    #endregion

    #region IAddChild Members

    public void AddChild(TriggerAction o)
    {
      Actions.Add(o);
    }

    #endregion
  }
}
