using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;
using SharpDX.XInput;

namespace Emulators.LibRetro.Controllers
{
  class XboxController : IRetroPad
  {
    protected Dictionary<SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags> _bindings;

    public XboxController()
    {
      InitMapping();
    }

    protected void InitMapping()
    {
      _bindings = new Dictionary<SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD, GamepadButtonFlags>();
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.UP] = GamepadButtonFlags.DPadUp;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.DOWN] = GamepadButtonFlags.DPadDown;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.LEFT] = GamepadButtonFlags.DPadLeft;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.RIGHT] = GamepadButtonFlags.DPadRight;

      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.SELECT] = GamepadButtonFlags.Back;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.START] = GamepadButtonFlags.Start;

      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.A] = GamepadButtonFlags.B;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.B] = GamepadButtonFlags.A;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.X] = GamepadButtonFlags.Y;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.Y] = GamepadButtonFlags.X;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.L] = GamepadButtonFlags.LeftShoulder;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.R] = GamepadButtonFlags.RightShoulder;
    }

    public bool IsButtonPressed(int port, SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD button)
    {
      var controller = GetController(port);
      if (controller.IsConnected)
      {
        GamepadButtonFlags buttonFlag;
        if (_bindings.TryGetValue(button, out buttonFlag))
        {
          return (controller.GetState().Gamepad.Buttons & buttonFlag) == buttonFlag;
        }
      }
      return false;
    }

    protected Controller GetController(int port)
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
