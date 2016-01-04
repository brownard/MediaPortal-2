using Emulators.Settings;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public enum RetroAnalogDevice
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

  public class RetroPadMapping
  {
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput> _buttonMappings;
    protected Dictionary<RetroAnalogDevice, DeviceInput> _analogMappings;

    public RetroPadMapping()
    {
      _buttonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>();
      _analogMappings = new Dictionary<RetroAnalogDevice, DeviceInput>();
    }
    
    public string DeviceName { get; set; }

    [XmlIgnore]
    public Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput> ButtonMappings
    {
      get { return _buttonMappings; }
    }

    [XmlIgnore]
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

    /// <summary>
    /// Used for serialization
    /// </summary>
    public SerializeableKeyValue<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>[] ButtonMappingsSerializable
    {
      get
      {
        var list = new List<SerializeableKeyValue<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>>();
        foreach (var kvp in _buttonMappings)
          list.Add(new SerializeableKeyValue<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>(kvp.Key, kvp.Value));
        return list.ToArray();
      }
      set
      {
        foreach (var item in value)
          _buttonMappings[item.Key] = item.Value;
      }
    }

    /// <summary>
    /// Used for serialization
    /// </summary>
    public SerializeableKeyValue<RetroAnalogDevice, DeviceInput>[] AnalogMappingsSerializable
    {
      get
      {
        var list = new List<SerializeableKeyValue<RetroAnalogDevice, DeviceInput>>();
        foreach (var kvp in _analogMappings)
          list.Add(new SerializeableKeyValue<RetroAnalogDevice, DeviceInput>(kvp.Key, kvp.Value));
        return list.ToArray();
      }
      set
      {
        foreach (var item in value)
          _analogMappings[item.Key] = item.Value;
      }
    }

  }
}
