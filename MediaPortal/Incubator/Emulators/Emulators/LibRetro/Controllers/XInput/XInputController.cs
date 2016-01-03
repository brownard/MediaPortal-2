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
  enum XInputAnalogInput
  {
    LeftThumbLeft,
    LeftThumbRight,
    LeftThumbUp,
    LeftThumbDown,
    RightThumbLeft,
    RightThumbRight,
    RightThumbUp,
    RightThumbDown,
    LeftTrigger,
    RightTrigger
  }

  class XInputController : IRetroPad, IRetroAnalog
  {
    const int MAX_CONTROLLERS = 4;
    const int CONTROLLER_CONNECTED_TIMEOUT = 2000;
    protected XInputControllerCache[] _controllers;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags> _buttonToButtonMappings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, XInputAnalogInput> _analogToButtonMappings;
    protected Dictionary<RetroAnalogDevice, XInputAnalogInput> _analogToAnalogMappings;
    protected Dictionary<RetroAnalogDevice, GamepadButtonFlags> _buttonToAnalogMappings;

    public XInputController(RetroPadMapping mapping)
    {
      InitControllers();
      InitMapping(mapping);
    }

    protected void InitControllers()
    {
      _controllers = new XInputControllerCache[4];
      _controllers[0] = new XInputControllerCache(new Controller(UserIndex.One));
      _controllers[1] = new XInputControllerCache(new Controller(UserIndex.Two));
      _controllers[2] = new XInputControllerCache(new Controller(UserIndex.Three));
      _controllers[3] = new XInputControllerCache(new Controller(UserIndex.Four));
    }

    protected void InitMapping(RetroPadMapping mapping)
    {
      _buttonToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags>();
      _analogToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, XInputAnalogInput>();
      _analogToAnalogMappings = new Dictionary<RetroAnalogDevice, XInputAnalogInput>();
      _buttonToAnalogMappings = new Dictionary<RetroAnalogDevice, GamepadButtonFlags>();

      foreach (var kvp in mapping.ButtonMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Digital)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToButtonMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Analog)
        {
          XInputAnalogInput analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToButtonMappings[kvp.Key] = analogInput;
        }
      }

      foreach (var kvp in mapping.AnalogMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Digital)
        {
          GamepadButtonFlags button;
          if (Enum.TryParse(deviceInput.Id, out button))
            _buttonToAnalogMappings[kvp.Key] = button;
        }
        else if (deviceInput.InputType == InputType.Analog)
        {
          XInputAnalogInput analogInput;
          if (Enum.TryParse(deviceInput.Id, out analogInput))
            _analogToAnalogMappings[kvp.Key] = analogInput;
        }
      }
    }

    public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      Gamepad gamepad;
      if (!TryGetGamepad(port, out gamepad))
        return false;

      GamepadButtonFlags buttonFlag;
      if (_buttonToButtonMappings.TryGetValue(button, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        return true;
      XInputAnalogInput analogInput;
      if (_analogToButtonMappings.TryGetValue(button, out analogInput) && IsAnalogPressed(analogInput, gamepad))
        return true;
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

      XInputAnalogInput analog;
      GamepadButtonFlags buttonFlag;
      if (_analogToAnalogMappings.TryGetValue(positive, out analog))
        positivePosition = GetAnalogPosition(analog, gamepad, true);
      else if (_buttonToAnalogMappings.TryGetValue(positive, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        positivePosition = short.MaxValue;

      if (_analogToAnalogMappings.TryGetValue(negative, out analog))
        negativePosition = GetAnalogPosition(analog, gamepad, false);
      else if (_buttonToAnalogMappings.TryGetValue(negative, out buttonFlag) && IsButtonPressed(buttonFlag, gamepad))
        negativePosition = short.MinValue;

      if (positivePosition != 0 && negativePosition == 0)
        return positivePosition;
      if (positivePosition == 0 && negativePosition != 0)
        return negativePosition;
      return 0;
    }

    protected bool TryGetGamepad(uint port, out Gamepad gamepad)
    {
      State state;
      if (port < MAX_CONTROLLERS && _controllers[port].GetState(CONTROLLER_CONNECTED_TIMEOUT, out state))
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

    public static bool IsAnalogPressed(XInputAnalogInput analogInput, Gamepad gamepad)
    {
      switch (analogInput)
      {
        case XInputAnalogInput.LeftThumbLeft:
          return gamepad.LeftThumbX < -Gamepad.LeftThumbDeadZone;
        case XInputAnalogInput.LeftThumbRight:
          return gamepad.LeftThumbX > Gamepad.LeftThumbDeadZone;
        case XInputAnalogInput.LeftThumbUp:
          return gamepad.LeftThumbY > Gamepad.LeftThumbDeadZone;
        case XInputAnalogInput.LeftThumbDown:
          return gamepad.LeftThumbY < -Gamepad.LeftThumbDeadZone;
        case XInputAnalogInput.RightThumbLeft:
          return gamepad.RightThumbX < -Gamepad.RightThumbDeadZone;
        case XInputAnalogInput.RightThumbRight:
          return gamepad.RightThumbX > Gamepad.RightThumbDeadZone;
        case XInputAnalogInput.RightThumbUp:
          return gamepad.RightThumbY > Gamepad.RightThumbDeadZone;
        case XInputAnalogInput.RightThumbDown:
          return gamepad.RightThumbY < -Gamepad.RightThumbDeadZone;
        case XInputAnalogInput.LeftTrigger:
          return gamepad.LeftTrigger > Gamepad.TriggerThreshold;
        case XInputAnalogInput.RightTrigger:
          return gamepad.RightTrigger > Gamepad.TriggerThreshold;
      }
      return false;
    }

    public static short GetAnalogPosition(XInputAnalogInput analogInput, Gamepad gamepad, bool isPositiveDirection)
    {
      short position = 0;
      bool shouldInvert = false;
      switch (analogInput)
      {
        case XInputAnalogInput.LeftThumbLeft:
          position = gamepad.LeftThumbX;
          if (position >= 0)
            return 0;
          shouldInvert = isPositiveDirection;
          break;
        case XInputAnalogInput.LeftThumbRight:
          position = gamepad.LeftThumbX;
          if (position <= 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
        case XInputAnalogInput.LeftThumbUp:
          position = gamepad.LeftThumbY;
          if (position <= 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
        case XInputAnalogInput.LeftThumbDown:
          position = gamepad.LeftThumbY;
          if (position >= 0)
            return 0;
          shouldInvert = isPositiveDirection;
          break;
        case XInputAnalogInput.RightThumbLeft:
          position = gamepad.RightThumbX;
          if (position >= 0)
            return 0;
          shouldInvert = isPositiveDirection;
          break;
        case XInputAnalogInput.RightThumbRight:
          position = gamepad.RightThumbX;
          if (position <= 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
        case XInputAnalogInput.RightThumbUp:
          position = gamepad.RightThumbY;
          if (position <= 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
        case XInputAnalogInput.RightThumbDown:
          position = gamepad.RightThumbY;
          if (position >= 0)
            return 0;
          shouldInvert = isPositiveDirection;
          break;
        case XInputAnalogInput.LeftTrigger:
          position = ScaleByteToShort(gamepad.LeftTrigger);
          if (position == 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
        case XInputAnalogInput.RightTrigger:
          position = ScaleByteToShort(gamepad.RightTrigger);
          if (position == 0)
            return 0;
          shouldInvert = !isPositiveDirection;
          break;
      }
      if (shouldInvert)
        position = (short)(-position - 1);
      return position;
    }

    static short ScaleByteToShort(byte b)
    {
      if (b == 0)
        return 0;
      return (short)(((b << 8) | b) >> 1);
    }
  }
}