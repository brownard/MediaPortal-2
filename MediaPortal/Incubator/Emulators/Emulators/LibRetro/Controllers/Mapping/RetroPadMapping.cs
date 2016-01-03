using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  enum RetroAnalogDevice
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

  class RetroPadMapping
  {
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput> _buttonMappings;
    protected Dictionary<RetroAnalogDevice, DeviceInput> _analogMappings;

    public RetroPadMapping()
    {
      _buttonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>();
      _analogMappings = new Dictionary<RetroAnalogDevice, DeviceInput>();
    }

    public Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput> ButtonMappings
    {
      get { return _buttonMappings; }
    }

    public Dictionary<RetroAnalogDevice, DeviceInput> AnalogMappings
    {
      get { return _analogMappings; }
    }

    public void MapButton(LibRetroCore.RETRO_DEVICE_ID_JOYPAD retroButton, DeviceInput deviceInput)
    {
      _buttonMappings[retroButton] = deviceInput;
    }

    public void MapAnalog(RetroAnalogDevice retroAnalog, DeviceInput deviceInput)
    {
      _analogMappings[retroAnalog] = deviceInput;
    }

    public static void GetAnalogEnums(LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction, out RetroAnalogDevice positive, out RetroAnalogDevice negative)
    {
      if (index == LibRetroCore.RETRO_DEVICE_INDEX_ANALOG.LEFT)
      {
        if (direction == LibRetroCore.RETRO_DEVICE_ID_ANALOG.X)
        {
          positive = RetroAnalogDevice.LeftThumbRight;
          negative = RetroAnalogDevice.LeftThumbLeft;
        }
        else
        {
          //Libretro defines positive Y values as down
          positive = RetroAnalogDevice.LeftThumbDown;
          negative = RetroAnalogDevice.LeftThumbUp;
        }
      }
      else
      {
        if (direction == LibRetroCore.RETRO_DEVICE_ID_ANALOG.X)
        {
          positive = RetroAnalogDevice.RightThumbRight;
          negative = RetroAnalogDevice.RightThumbLeft;
        }
        else
        {
          //Libretro defines positive Y values as down
          positive = RetroAnalogDevice.RightThumbDown;
          negative = RetroAnalogDevice.RightThumbUp;
        }
      }
    }
  }
}
