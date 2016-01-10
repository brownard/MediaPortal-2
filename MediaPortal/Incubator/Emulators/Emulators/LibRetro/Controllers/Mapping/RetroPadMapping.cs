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

  public class MappedInput
  {
    public string Name { get; set; }
    public LibRetroCore.RETRO_DEVICE_ID_JOYPAD? Button { get; set; }
    public RetroAnalogDevice? Analog { get; set; }
    public DeviceInput Input { get; set; }
  }

  public class RetroPadMapping
  {
    protected List<MappedInput> _availableInputs;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput> _buttonMappings;
    protected Dictionary<RetroAnalogDevice, DeviceInput> _analogMappings;

    public RetroPadMapping()
    {
      _availableInputs = GetAvailableInputs();
      _buttonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DeviceInput>();
      _analogMappings = new Dictionary<RetroAnalogDevice, DeviceInput>();
    }
    
    public Guid DeviceId { get; set; }
    public string SubDeviceId { get; set; }
    public string DeviceName { get; set; }

    [XmlIgnore]
    public List<MappedInput> AvailableInputs
    {
      get { return _availableInputs; }
    }

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

    public void Map(MappedInput mappedInput)
    {
      if (mappedInput.Button.HasValue)
        MapButton(mappedInput.Button.Value, mappedInput.Input);
      else if (mappedInput.Analog.HasValue)
        MapAnalog(mappedInput.Analog.Value, mappedInput.Input);
    }

    public void MapButton(LibRetroCore.RETRO_DEVICE_ID_JOYPAD retroButton, DeviceInput deviceInput)
    {
      _buttonMappings[retroButton] = deviceInput;
    }

    public void MapAnalog(RetroAnalogDevice retroAnalog, DeviceInput deviceInput)
    {
      _analogMappings[retroAnalog] = deviceInput;
    }

    public List<MappedInput> GetAvailableInputs()
    {
      List<MappedInput> inputList = new List<MappedInput>();
      inputList.Add(new MappedInput() { Name = "Up", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.UP });
      inputList.Add(new MappedInput() { Name = "Down", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.DOWN });
      inputList.Add(new MappedInput() { Name = "Left", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.LEFT });
      inputList.Add(new MappedInput() { Name = "Right", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.RIGHT });

      inputList.Add(new MappedInput() { Name = "Left Analog X- (Left)", Analog = RetroAnalogDevice.LeftThumbLeft });
      inputList.Add(new MappedInput() { Name = "Left Analog X+ (Right)", Analog = RetroAnalogDevice.LeftThumbRight });
      inputList.Add(new MappedInput() { Name = "Left Analog Y- (Up)", Analog = RetroAnalogDevice.LeftThumbUp });
      inputList.Add(new MappedInput() { Name = "Left Analog Y+ (Down)", Analog = RetroAnalogDevice.LeftThumbDown });
      inputList.Add(new MappedInput() { Name = "Right Analog X- (Left)", Analog = RetroAnalogDevice.RightThumbLeft });
      inputList.Add(new MappedInput() { Name = "Right Analog X+ (Right)", Analog = RetroAnalogDevice.RightThumbRight });
      inputList.Add(new MappedInput() { Name = "Right Analog Y- (Up)", Analog = RetroAnalogDevice.RightThumbUp });
      inputList.Add(new MappedInput() { Name = "Right Analog Y+ (Down)", Analog = RetroAnalogDevice.RightThumbDown });

      inputList.Add(new MappedInput() { Name = "A", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.A });
      inputList.Add(new MappedInput() { Name = "B", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.B });
      inputList.Add(new MappedInput() { Name = "X", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.X });
      inputList.Add(new MappedInput() { Name = "Y", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.Y });
      inputList.Add(new MappedInput() { Name = "Start", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.START });
      inputList.Add(new MappedInput() { Name = "Select", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.SELECT });
      inputList.Add(new MappedInput() { Name = "L1", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L });
      inputList.Add(new MappedInput() { Name = "R1", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R });
      inputList.Add(new MappedInput() { Name = "L2", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L2 });
      inputList.Add(new MappedInput() { Name = "R2", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R2 });
      inputList.Add(new MappedInput() { Name = "L3", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.L3 });
      inputList.Add(new MappedInput() { Name = "R3", Button = LibRetroCore.RETRO_DEVICE_ID_JOYPAD.R3 });
      return inputList;
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
