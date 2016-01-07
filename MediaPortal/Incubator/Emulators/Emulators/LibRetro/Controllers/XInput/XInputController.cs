using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XInput;
using SharpRetro.LibRetro;
using Emulators.LibRetro.Controllers.Mapping;

namespace Emulators.LibRetro.Controllers.XInput
{
  enum XInputAxisType
  {
    LeftThumbX,
    LeftThumbY,
    RightThumbX,
    RightThumbY,
    LeftTrigger,
    RightTrigger
  }

  class XInputAxis
  {
    public XInputAxis(XInputAxisType axisType, bool positiveValues)
    {
      AxisType = axisType;
      PositiveValues = positiveValues;
    }

    public XInputAxisType AxisType { get; set; }
    public bool PositiveValues { get; set; }
  }

  class XInputController : IRetroPad, IRetroAnalog, IRetroRumble
  {
    const int MAX_CONTROLLERS = 4;
    const int CONTROLLER_CONNECTED_TIMEOUT = 2000;
    protected XInputControllerCache[] _controllers;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags> _buttonToButtonMappings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, XInputAxis> _analogToButtonMappings;
    protected Dictionary<RetroAnalogDevice, XInputAxis> _analogToAnalogMappings;
    protected Dictionary<RetroAnalogDevice, GamepadButtonFlags> _buttonToAnalogMappings;

    public XInputController(RetroPadMapping mapping)
    {
      InitControllers();
      InitMapping(mapping);
    }

    protected void InitControllers()
    {
      _controllers = new XInputControllerCache[4];
      _controllers[0] = new XInputControllerCache(new Controller(UserIndex.One), CONTROLLER_CONNECTED_TIMEOUT);
      _controllers[1] = new XInputControllerCache(new Controller(UserIndex.Two), CONTROLLER_CONNECTED_TIMEOUT);
      _controllers[2] = new XInputControllerCache(new Controller(UserIndex.Three), CONTROLLER_CONNECTED_TIMEOUT);
      _controllers[3] = new XInputControllerCache(new Controller(UserIndex.Four), CONTROLLER_CONNECTED_TIMEOUT);
    }

    protected void InitMapping(RetroPadMapping mapping)
    {
      _buttonToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags>();
      _analogToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, XInputAxis>();
      _analogToAnalogMappings = new Dictionary<RetroAnalogDevice, XInputAxis>();
      _buttonToAnalogMappings = new Dictionary<RetroAnalogDevice, GamepadButtonFlags>();

      foreach (var kvp in mapping.ButtonMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToButtonMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          XInputAxisType analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToButtonMappings[kvp.Key] = new XInputAxis(analogInput, deviceInput.PositiveValues);
        }
      }

      foreach (var kvp in mapping.AnalogMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToAnalogMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          XInputAxisType analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToAnalogMappings[kvp.Key] = new XInputAxis(analogInput, deviceInput.PositiveValues);
        }
      }
    }

    public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      Gamepad gamepad;
      if (!TryGetGamepad(port, out gamepad))
        return false;

      GamepadButtonFlags buttonFlag;
      if (_buttonToButtonMappings.TryGetValue(button, out buttonFlag))
        return IsButtonPressed(buttonFlag, gamepad);
      XInputAxis axis;
      if (_analogToButtonMappings.TryGetValue(button, out axis))
        return IsAxisPressed(axis, gamepad);
      return false;
    }

    public short GetAnalog(uint port, LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction)
    {
      Gamepad gamepad;
      if (!TryGetGamepad(port, out gamepad))
        return 0;

      RetroAnalogDevice positive;
      RetroAnalogDevice negative;
      RetroPadMapping.GetAnalogEnums(index, direction, out positive, out negative);
      short positivePosition = 0;
      short negativePosition = 0;

      XInputAxis axis;
      GamepadButtonFlags buttonFlag;
      if (_analogToAnalogMappings.TryGetValue(positive, out axis))
        positivePosition = GetAxisPositionMapped(axis, gamepad, true);
      else if (_buttonToAnalogMappings.TryGetValue(positive, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        positivePosition = short.MaxValue;

      if (_analogToAnalogMappings.TryGetValue(negative, out axis))
        negativePosition = GetAxisPositionMapped(axis, gamepad, false);
      else if (_buttonToAnalogMappings.TryGetValue(negative, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        negativePosition = short.MinValue;

      if (positivePosition != 0 && negativePosition == 0)
        return positivePosition;
      if (positivePosition == 0 && negativePosition != 0)
        return negativePosition;
      return 0;
    }

    public bool SetRumbleState(uint port, LibRetroCore.retro_rumble_effect effect, ushort strength)
    {
      if (port >= MAX_CONTROLLERS)
        return false;
      XInputControllerCache controller = _controllers[port];
      if (!controller.IsConnected())
        return false;

      controller.Controller.SetVibration(new Vibration()
      {
        LeftMotorSpeed = strength,
        RightMotorSpeed = strength
      });
      return true;
    }

    protected bool TryGetGamepad(uint port, out Gamepad gamepad)
    {
      State state;
      if (port < MAX_CONTROLLERS && _controllers[port].GetState(out state))
      {
        gamepad = state.Gamepad;
        return true;
      }
      gamepad = default(Gamepad);
      return false;
    }

    public static bool IsButtonPressed(GamepadButtonFlags buttonFlag, Gamepad gamepad)
    {
      return (gamepad.Buttons & buttonFlag) == buttonFlag;
    }

    public static bool IsAxisPressed(XInputAxis axis, Gamepad gamepad)
    {
      short axisValue = 0;
      short deadZone = 0;
      switch (axis.AxisType)
      {
        case XInputAxisType.LeftThumbX:
          axisValue = gamepad.LeftThumbX;
          deadZone = Gamepad.LeftThumbDeadZone;
          break;
        case XInputAxisType.LeftThumbY:
          axisValue = gamepad.LeftThumbY;
          deadZone = Gamepad.LeftThumbDeadZone;
          break;
        case XInputAxisType.RightThumbX:
          axisValue = gamepad.RightThumbX;
          deadZone = Gamepad.RightThumbDeadZone;
          break;
        case XInputAxisType.RightThumbY:
          axisValue = gamepad.RightThumbY;
          deadZone = Gamepad.RightThumbDeadZone;
          break;
        case XInputAxisType.LeftTrigger:
          axisValue = gamepad.LeftTrigger;
          deadZone = Gamepad.TriggerThreshold;
          break;
        case XInputAxisType.RightTrigger:
          axisValue = gamepad.RightTrigger;
          deadZone = Gamepad.TriggerThreshold;
          break;
      }

      return axis.PositiveValues ? axisValue > deadZone : axisValue < -deadZone;
    }

    public static short GetAxisPosition(XInputAxisType axisType, Gamepad gamepad)
    {
      switch (axisType)
      {
        case XInputAxisType.LeftThumbX:
          return gamepad.LeftThumbX;
        case XInputAxisType.LeftThumbY:
          return gamepad.LeftThumbY;
        case XInputAxisType.RightThumbX:
          return gamepad.RightThumbX;
        case XInputAxisType.RightThumbY:
          return gamepad.RightThumbY;
        case XInputAxisType.LeftTrigger:
          return NumericUtils.ScaleByteToShort(gamepad.LeftTrigger);
        case XInputAxisType.RightTrigger:
          return NumericUtils.ScaleByteToShort(gamepad.RightTrigger);
        default:
          return 0;
      }
    }

    public static short GetAxisPositionMapped(XInputAxis axis, Gamepad gamepad, bool isMappedToPositive)
    {
      short position = GetAxisPosition(axis.AxisType, gamepad);
      if (position == 0 || (axis.PositiveValues && position <= 0) || (!axis.PositiveValues && position >= 0))
        return 0;

      bool shouldInvert = (axis.PositiveValues && !isMappedToPositive) || (!axis.PositiveValues && isMappedToPositive);
      if (shouldInvert)
        position = (short)(-position - 1);
      return position;
    }
  }
}