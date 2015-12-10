using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XInput;
using SharpRetro.LibRetro;

namespace Emulators.LibRetro.Controllers
{
  enum AxisDirection
  {
    LeftThumbLeft,
    LeftThumbRight,
    LeftThumbUp,
    LeftThumbDown,
    RightThumbLeft,
    RightThumbRight,
    RightThumbUp,
    RightThumbDown,
  }

  enum Trigger
  {
    Left,
    Right
  }

  class XInputController : IRetroPad, IRetroAnalog
  {
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags> _buttonBindings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, AxisDirection> _axisBindings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, Trigger> _triggerBindings;
    protected bool _bindAnalogToDPad;

    public XInputController(bool bindAnalogToDPad)
    {
      _bindAnalogToDPad = bindAnalogToDPad;
      InitDefaultMapping();
    }

    protected void InitDefaultMapping()
    {
      _buttonBindings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags>();
      _axisBindings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, AxisDirection>();
      _triggerBindings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, Trigger>();

      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP] = GamepadButtonFlags.DPadUp;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN] = GamepadButtonFlags.DPadDown;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT] = GamepadButtonFlags.DPadLeft;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT] = GamepadButtonFlags.DPadRight;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.SELECT] = GamepadButtonFlags.Back;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.START] = GamepadButtonFlags.Start;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.A] = GamepadButtonFlags.B;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.B] = GamepadButtonFlags.A;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.X] = GamepadButtonFlags.Y;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.Y] = GamepadButtonFlags.X;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L] = GamepadButtonFlags.LeftShoulder;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R] = GamepadButtonFlags.RightShoulder;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L3] = GamepadButtonFlags.LeftThumb;
      _buttonBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R3] = GamepadButtonFlags.RightThumb;
      _triggerBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L2] = Trigger.Left;
      _triggerBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R2] = Trigger.Right;

      if (_bindAnalogToDPad)
      {
        _axisBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP] = AxisDirection.LeftThumbUp;
        _axisBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN] = AxisDirection.LeftThumbDown;
        _axisBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT] = AxisDirection.LeftThumbLeft;
        _axisBindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT] = AxisDirection.LeftThumbRight;
      }
    }

    public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      var controller = GetController(port);
      if (!controller.IsConnected)
        return false;
      Gamepad gamepad = controller.GetState().Gamepad;

      GamepadButtonFlags buttonFlag;
      if (_buttonBindings.TryGetValue(button, out buttonFlag)
        && (gamepad.Buttons & buttonFlag) == buttonFlag)
        return true;

      AxisDirection axisDirection;
      if (_axisBindings.TryGetValue(button, out axisDirection)
        && IsAxisPressed(axisDirection, gamepad))
        return true;

      Trigger trigger;
      if (_triggerBindings.TryGetValue(button, out trigger)
        && IsTriggerPressed(trigger, gamepad))
        return true;

      return false;
    }

    public short GetAnalog(uint port, LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction)
    {
      var controller = GetController(port);
      if (!controller.IsConnected)
        return 0;
      Gamepad gamepad = controller.GetState().Gamepad;
      //LibRetro specifies that positive Y values are down, so we need to invert the Y values reported by XInput
      if (index == LibRetroCore.RETRO_DEVICE_INDEX_ANALOG.LEFT)
        return direction == LibRetroCore.RETRO_DEVICE_ID_ANALOG.X ? gamepad.LeftThumbX : (short)(-gamepad.LeftThumbY - 1);
      else
        return direction == LibRetroCore.RETRO_DEVICE_ID_ANALOG.X ? gamepad.RightThumbX : (short)(-gamepad.RightThumbY - 1);
    }

    protected bool IsTriggerPressed(Trigger trigger, Gamepad gamepad)
    {
      if (trigger == Trigger.Left)
        return gamepad.LeftTrigger > Gamepad.TriggerThreshold;
      return gamepad.RightTrigger > Gamepad.TriggerThreshold;
    }

    protected bool IsAxisPressed(AxisDirection axisDirection, Gamepad gamepad)
    {
      switch (axisDirection)
      {
        case AxisDirection.LeftThumbLeft:
          return gamepad.LeftThumbX < 0 - Gamepad.LeftThumbDeadZone;
        case AxisDirection.LeftThumbRight:
          return gamepad.LeftThumbX > Gamepad.LeftThumbDeadZone;
        case AxisDirection.LeftThumbUp:
          return gamepad.LeftThumbY > Gamepad.LeftThumbDeadZone;
        case AxisDirection.LeftThumbDown:
          return gamepad.LeftThumbY < 0 - Gamepad.LeftThumbDeadZone;
        case AxisDirection.RightThumbLeft:
          return gamepad.RightThumbX < 0 - Gamepad.RightThumbDeadZone;
        case AxisDirection.RightThumbRight:
          return gamepad.RightThumbX > Gamepad.RightThumbDeadZone;
        case AxisDirection.RightThumbUp:
          return gamepad.RightThumbY > Gamepad.RightThumbDeadZone;
        case AxisDirection.RightThumbDown:
          return gamepad.RightThumbY < 0 - Gamepad.RightThumbDeadZone;
      }
      return false;
    }

    protected Controller GetController(uint port)
    {
      UserIndex index;
      switch (port)
      {
        case 0:
          index = UserIndex.One;
          break;
        case 1:
          index = UserIndex.Two;
          break;
        case 2:
          index = UserIndex.Three;
          break;
        case 4:
          index = UserIndex.Four;
          break;
        default:
          index = UserIndex.One;
          break;
      }
      return new Controller(index);
    }
  }
}
