using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpRetro.LibRetro;
using LibRetro = SharpRetro.LibRetro.LibRetroCore;

namespace Emulators.LibRetro.Controllers
{
  class KeyboardController : IRetroPad
  {
    protected uint _port;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, Keys> _bindings;

    public KeyboardController()
    {
      _bindings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, Keys>();
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP] = Keys.Up;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN] = Keys.Down;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT] = Keys.Left;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT] = Keys.Right;

      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.SELECT] = Keys.N;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.START] = Keys.M;

      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.A] = Keys.A;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.B] = Keys.S;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.X] = Keys.Z;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.Y] = Keys.X;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L] = Keys.Q;
      _bindings[LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R] = Keys.E;
    }

    public bool IsButtonPressed(uint port, SharpRetro.LibRetro.LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      Keys key;
      return port == _port && _bindings.TryGetValue(button, out key) && Keyboard.IsKeyDown(key);
    }
  }
}
