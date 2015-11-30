using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpRetro.LibRetro;
using LibRetro = SharpRetro.LibRetro.LibRetro;

namespace Emulators.LibRetro.Controllers
{
  class KeyboardController : IRetroPad
  {
    protected int _port;
    protected Dictionary<SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD, Keys> _bindings;

    public KeyboardController()
    {
      _bindings = new Dictionary<SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD, Keys>();
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.UP] = Keys.Up;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.DOWN] = Keys.Down;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.LEFT] = Keys.Left;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.RIGHT] = Keys.Right;

      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.SELECT] = Keys.N;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.START] = Keys.M;

      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.A] = Keys.A;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.B] = Keys.S;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.X] = Keys.Z;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.Y] = Keys.X;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.L] = Keys.Q;
      _bindings[SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD.R] = Keys.E;
    }

    public bool IsButtonPressed(int port, SharpRetro.LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD button)
    {
      Keys key;
      return port == _port && _bindings.TryGetValue(button, out key) && Keyboard.IsKeyDown(key);
    }
  }
}
