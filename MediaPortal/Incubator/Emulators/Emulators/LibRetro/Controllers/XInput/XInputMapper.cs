using Emulators.LibRetro.Controllers.Mapping;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.XInput
{
  class XInputMapper : IInputDeviceMapper
  {
    #region Available Inputs
    static readonly DeviceInput DPAD_LEFT = new DeviceInput("D-Pad Left", GamepadButtonFlags.DPadLeft.ToString(), InputType.Digital);
    static readonly DeviceInput DPAD_RIGHT = new DeviceInput("D-Pad Right", GamepadButtonFlags.DPadRight.ToString(), InputType.Digital);
    static readonly DeviceInput DPAD_UP = new DeviceInput("D-Pad Up", GamepadButtonFlags.DPadUp.ToString(), InputType.Digital);
    static readonly DeviceInput DPAD_DOWN = new DeviceInput("D-Pad Down", GamepadButtonFlags.DPadDown.ToString(), InputType.Digital);
    static readonly DeviceInput BACK = new DeviceInput("Back", GamepadButtonFlags.Back.ToString(), InputType.Digital);
    static readonly DeviceInput START = new DeviceInput("Start", GamepadButtonFlags.Start.ToString(), InputType.Digital);
    static readonly DeviceInput A = new DeviceInput("A", GamepadButtonFlags.A.ToString(), InputType.Digital);
    static readonly DeviceInput B = new DeviceInput("B", GamepadButtonFlags.B.ToString(), InputType.Digital);
    static readonly DeviceInput X = new DeviceInput("X", GamepadButtonFlags.X.ToString(), InputType.Digital);
    static readonly DeviceInput Y = new DeviceInput("Y", GamepadButtonFlags.Y.ToString(), InputType.Digital);
    static readonly DeviceInput LEFT_SHOULDER = new DeviceInput("Left Shoulder", GamepadButtonFlags.LeftShoulder.ToString(), InputType.Digital);
    static readonly DeviceInput RIGHT_SHOULDER = new DeviceInput("Right Shoulder", GamepadButtonFlags.RightShoulder.ToString(), InputType.Digital);
    static readonly DeviceInput LEFT_THUMB = new DeviceInput("Left Thumb", GamepadButtonFlags.LeftThumb.ToString(), InputType.Digital);
    static readonly DeviceInput RIGHT_THUMB = new DeviceInput("Right Thumb", GamepadButtonFlags.RightThumb.ToString(), InputType.Digital);

    static readonly DeviceInput LEFT_THUMB_LEFT = new DeviceInput("Left Thumb X-", XInputAnalogInput.LeftThumbLeft.ToString(), InputType.Analog);
    static readonly DeviceInput LEFT_THUMB_RIGHT = new DeviceInput("Left Thumb X+", XInputAnalogInput.LeftThumbRight.ToString(), InputType.Analog);
    static readonly DeviceInput LEFT_THUMB_UP = new DeviceInput("Left Thumb Y+", XInputAnalogInput.LeftThumbUp.ToString(), InputType.Analog);
    static readonly DeviceInput LEFT_THUMB_DOWN = new DeviceInput("Left Thumb Y-", XInputAnalogInput.LeftThumbDown.ToString(), InputType.Analog);
    static readonly DeviceInput RIGHT_THUMB_LEFT = new DeviceInput("Right Thumb X-", XInputAnalogInput.RightThumbLeft.ToString(), InputType.Analog);
    static readonly DeviceInput RIGHT_THUMB_RIGHT = new DeviceInput("Right Thumb X+", XInputAnalogInput.RightThumbRight.ToString(), InputType.Analog);
    static readonly DeviceInput RIGHT_THUMB_UP = new DeviceInput("Right Thumb Y+", XInputAnalogInput.RightThumbUp.ToString(), InputType.Analog);
    static readonly DeviceInput RIGHT_THUMB_DOWN = new DeviceInput("Right Thumb Y-", XInputAnalogInput.RightThumbDown.ToString(), InputType.Analog);
    static readonly DeviceInput LEFT_TRIGGER = new DeviceInput("Left Trigger", XInputAnalogInput.LeftTrigger.ToString(), InputType.Analog);
    static readonly DeviceInput RIGHT_TRIGGER = new DeviceInput("Right Trigger", XInputAnalogInput.RightTrigger.ToString(), InputType.Analog);

    protected static readonly DeviceInput[] AVAILABLE_INPUTS =
    {
      DPAD_LEFT,
      DPAD_RIGHT,
      DPAD_UP,
      DPAD_DOWN,
      BACK,
      START,
      A,
      B,
      X,
      Y,
      LEFT_SHOULDER,
      RIGHT_SHOULDER,
      LEFT_THUMB,
      RIGHT_THUMB,
      LEFT_THUMB_LEFT,
      LEFT_THUMB_RIGHT,
      LEFT_THUMB_UP,
      LEFT_THUMB_DOWN,
      RIGHT_THUMB_LEFT,
      RIGHT_THUMB_RIGHT,
      RIGHT_THUMB_UP,
      RIGHT_THUMB_DOWN,
      LEFT_TRIGGER,
      RIGHT_TRIGGER
    };
    #endregion

    #region DefaultMapping
    public static RetroPadMapping GetDefaultMapping(bool mapAnalogToDPad)
    {
      RetroPadMapping mapping = new RetroPadMapping();
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT, DPAD_LEFT);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT, DPAD_RIGHT);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP, DPAD_UP);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN, DPAD_DOWN);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.SELECT, BACK);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.START, START);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.A, B);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.B, A);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.X, Y);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.Y, X);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L, LEFT_SHOULDER);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R, RIGHT_SHOULDER);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L2, LEFT_TRIGGER);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R2, RIGHT_TRIGGER);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L3, LEFT_THUMB);
      mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R3, RIGHT_THUMB);

      mapping.MapAnalog(RetroAnalogDevice.RightThumbLeft, RIGHT_THUMB_LEFT);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbRight, RIGHT_THUMB_RIGHT);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbUp, RIGHT_THUMB_UP);
      mapping.MapAnalog(RetroAnalogDevice.RightThumbDown, RIGHT_THUMB_DOWN);

      if (mapAnalogToDPad)
      {
        mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT, LEFT_THUMB_LEFT);
        mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT, LEFT_THUMB_RIGHT);
        mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP, LEFT_THUMB_UP);
        mapping.MapButton(SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN, LEFT_THUMB_DOWN);
      }
      else
      {
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbLeft, LEFT_THUMB_LEFT);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbRight, LEFT_THUMB_RIGHT);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbUp, LEFT_THUMB_UP);
        mapping.MapAnalog(RetroAnalogDevice.LeftThumbDown, LEFT_THUMB_DOWN);
      }

      return mapping;
    }
    #endregion

    const int CONTROLLER_CONNECTED_TIMEOUT = 1000;
    protected Dictionary<string, DeviceInput> _inputs;
    protected string _deviceName;
    protected XInputControllerCache _controller;

    public XInputMapper(Controller controller)
    {
      _controller = new XInputControllerCache(controller);
      _deviceName = "XInput Device " + controller.UserIndex;
    }

    public string DeviceName
    {
      get { return _deviceName; }
    }

    public DeviceInput GetPressedInput()
    {
      State state;
      if (!_controller.GetState(CONTROLLER_CONNECTED_TIMEOUT, out state))
        return null;
      Gamepad gamepad = state.Gamepad;

      DeviceInput pressedInput;
      if (_inputs.TryGetValue(gamepad.Buttons.ToString(), out pressedInput))
        return pressedInput;

      foreach (XInputAnalogInput analog in Enum.GetValues(typeof(XInputAnalogInput)))
        if (_inputs.TryGetValue(analog.ToString(), out pressedInput) && XInputController.IsAnalogPressed(analog, gamepad))
          return pressedInput;

      return null;
    }

    protected void InitializeInputs()
    {
      _inputs = new Dictionary<string, DeviceInput>();
      foreach (DeviceInput input in AVAILABLE_INPUTS)
        _inputs.Add(input.Id, input);
    }
  }
}