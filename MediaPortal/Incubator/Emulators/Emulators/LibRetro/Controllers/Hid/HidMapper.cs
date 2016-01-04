using Emulators.LibRetro.Controllers.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  class HidMapper : IInputDeviceMapper
  {
    protected string _deviceName;
    protected HidListener _hidListener;
    protected HidState _currentState;

    public HidMapper(string deviceName, HidListener hidListener)
    {
      _deviceName = deviceName;
      _hidListener = hidListener;
      _hidListener.StateChanged += HidListener_StateChanged;
    }

    protected void HidListener_StateChanged(object sender, HidStateEventArgs e)
    {
      _currentState = e.State;
    }

    public string DeviceName
    {
      get { return _deviceName; }
    }

    public DeviceInput GetPressedInput()
    {
      HidState state = _currentState;
      if (state == null)
        return null;

      if (state.Buttons.Count > 0)
      {
        string buttonId = state.Buttons.First().ToString();
        return new DeviceInput(buttonId, buttonId, InputType.Button);
      }

      if (IsDirectionPadStateValid(state.DirectionPadState))
      {
        string direction = state.DirectionPadState.ToString();
        return new DeviceInput(direction, direction, InputType.Button);
      }

      foreach (HidAxisState axis in state.AxisStates.Values)
      {
        short value = NumericUtils.UIntToShort(axis.Value);
        if (value > HidGameControl.AXIS_DEADZONE || value < -HidGameControl.AXIS_DEADZONE)
          return new DeviceInput(axis.Name, axis.Id.ToString(), InputType.Axis, value > 0);
      }

      return null;
    }

    protected bool IsDirectionPadStateValid(SharpLib.Hid.DirectionPadState directionPadState)
    {
      return directionPadState == SharpLib.Hid.DirectionPadState.Up
        || directionPadState == SharpLib.Hid.DirectionPadState.Right
        || directionPadState == SharpLib.Hid.DirectionPadState.Down
        || directionPadState == SharpLib.Hid.DirectionPadState.Left;
    }
  }
}
