using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.Controller
{
  public interface IRetroController
  {
  }

  public interface IRetroPad : IRetroController
  {
    bool IsButtonPressed(int port, LibRetro.LibRetro.RETRO_DEVICE_ID_JOYPAD button);
  }

  public interface IRetroKeyboard : IRetroController
  {
    bool IsKeyPressed(LibRetro.LibRetro.RETRO_KEY key);
  }

  public interface IRetroPointer : IRetroController
  {
    short GetPointerX();
    short GetPointerY();
    bool IsPointerPressed();
  }
}
