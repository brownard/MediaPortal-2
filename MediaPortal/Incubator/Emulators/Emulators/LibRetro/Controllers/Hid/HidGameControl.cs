using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;
using Emulators.LibRetro.Controllers.Mapping;
using SharpLib.Hid;

namespace Emulators.LibRetro.Controllers.Hid
{
  class HidAxis
  {
    public HidAxis(ushort axis, bool positiveValues)
    {
      Axis = axis;
      PositiveValues = positiveValues;
    }

    public ushort Axis { get; set; }
    public bool PositiveValues { get; set; }
  }

  class HidGameControl : IRetroPad, IRetroAnalog, IHidDevice
  {
    public const int AXIS_DEADZONE = 8192;
    
    protected HidState _currentState;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, ushort> _buttonToButtonMappings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, HidAxis> _analogToButtonMappings;
    protected Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DirectionPadState> _directionPadToButtonMappings;
    protected Dictionary<RetroAnalogDevice, HidAxis> _analogToAnalogMappings;
    protected Dictionary<RetroAnalogDevice, ushort> _buttonToAnalogMappings;
    protected Dictionary<RetroAnalogDevice, DirectionPadState> _directionPadToAnalogMappings;

    public HidGameControl(RetroPadMapping mapping)
    {
      InitializeMappings(mapping);
    }

    protected void InitializeMappings(RetroPadMapping mapping)
    {
      _buttonToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, ushort>();
      _analogToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, HidAxis>();
      _directionPadToButtonMappings = new Dictionary<LibRetroCore.RETRO_DEVICE_ID_JOYPAD, DirectionPadState>();
      _analogToAnalogMappings = new Dictionary<RetroAnalogDevice, HidAxis>();
      _buttonToAnalogMappings = new Dictionary<RetroAnalogDevice, ushort>();
      _directionPadToAnalogMappings = new Dictionary<RetroAnalogDevice, DirectionPadState>();

      foreach (var kvp in mapping.ButtonMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          ushort button;
          DirectionPadState directionPadState;
          if (ushort.TryParse(deviceInput.Id, out button))
            _buttonToButtonMappings.Add(kvp.Key, button);
          else if (Enum.TryParse(deviceInput.Id, out directionPadState))
            _directionPadToButtonMappings.Add(kvp.Key, directionPadState);
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          ushort axis;
          if (ushort.TryParse(deviceInput.Id, out axis))
            _analogToButtonMappings.Add(kvp.Key, new HidAxis(axis, deviceInput.PositiveValues));
        }
      }

      foreach (var kvp in mapping.AnalogMappings)
      {
        DeviceInput deviceInput = kvp.Value;
        if (deviceInput.InputType == InputType.Button)
        {
          ushort button;
          DirectionPadState directionPadState;
          if (ushort.TryParse(deviceInput.Id, out button))
            _buttonToAnalogMappings.Add(kvp.Key, button);
          else if (Enum.TryParse(deviceInput.Id, out directionPadState))
            _directionPadToAnalogMappings.Add(kvp.Key, directionPadState);
        }
        else if (deviceInput.InputType == InputType.Axis)
        {
          ushort axis;
          if (ushort.TryParse(deviceInput.Id, out axis))
            _analogToAnalogMappings.Add(kvp.Key, new HidAxis(axis, deviceInput.PositiveValues));
        }
      }
    }

    public void UpdateState(HidState state)
    {
      _currentState = state;
    }

    public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      HidState state = _currentState;
      if (state == null)
        return false;

      ushort hidButton;
      if (_buttonToButtonMappings.TryGetValue(button, out hidButton))
        return IsButtonPressed(hidButton, state);

      DirectionPadState directionPadState;
      if (_directionPadToButtonMappings.TryGetValue(button, out directionPadState))
        return IsDirectionPadPressed(directionPadState, state);

      HidAxis axis;
      if (_analogToButtonMappings.TryGetValue(button, out axis))
        return IsAxisPressed(axis, state);

      return false;
    }

    public short GetAnalog(uint port, LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction)
    {
      HidState state = _currentState;
      if (state == null)
        return 0;

      RetroAnalogDevice positive;
      RetroAnalogDevice negative;
      RetroPadMapping.GetAnalogEnums(index, direction, out positive, out negative);
      short positivePosition = 0;
      short negativePosition = 0;

      HidAxis axis;
      ushort button;
      DirectionPadState directionPadState;
      if (_analogToAnalogMappings.TryGetValue(positive, out axis))
        positivePosition = GetAxisPositionMapped(axis, state, true);
      else if (_directionPadToAnalogMappings.TryGetValue(positive, out directionPadState) && IsDirectionPadPressed(directionPadState, state))
        positivePosition = short.MaxValue;
      else if (_buttonToAnalogMappings.TryGetValue(positive, out button) && IsButtonPressed(button, state))
        positivePosition = short.MaxValue;

      if (_analogToAnalogMappings.TryGetValue(negative, out axis))
        negativePosition = GetAxisPositionMapped(axis, state, false);
      else if (_directionPadToAnalogMappings.TryGetValue(negative, out directionPadState) && IsDirectionPadPressed(directionPadState, state))
        positivePosition = short.MinValue;
      else if (_buttonToAnalogMappings.TryGetValue(negative, out button) && IsButtonPressed(button, state))
        negativePosition = short.MinValue;

      if (positivePosition != 0 && negativePosition == 0)
        return positivePosition;
      if (positivePosition == 0 && negativePosition != 0)
        return negativePosition;
      return 0;
    }

    public static bool IsButtonPressed(ushort button, HidState state)
    {
      return state.Buttons.Contains(button);
    }

    public static bool IsDirectionPadPressed(DirectionPadState directionPadState, HidState state)
    {
      return state.DirectionPadState == directionPadState;
    }

    public static bool IsAxisPressed(HidAxis axis, HidState state)
    {
      HidAxisState axisState;
      if (!state.AxisStates.TryGetValue(axis.Axis, out axisState))
        return false;
      short value = NumericUtils.UIntToShort(axisState.Value);
      return axis.PositiveValues ? value > AXIS_DEADZONE : value < -AXIS_DEADZONE;
    }

    public static short GetAxisPosition(HidAxis axis, HidState state)
    {
      HidAxisState axisState;
      if (!state.AxisStates.TryGetValue(axis.Axis, out axisState))
        return 0;
      return NumericUtils.UIntToShort(axisState.Value);
    }

    public static short GetAxisPositionMapped(HidAxis axis, HidState state, bool isMappedToPositive)
    {
      short position = GetAxisPosition(axis, state);
      if (position == 0 || (axis.PositiveValues && position <= 0) || (!axis.PositiveValues && position >= 0))
        return 0;

      bool shouldInvert = (axis.PositiveValues && !isMappedToPositive) || (!axis.PositiveValues && isMappedToPositive);
      if (shouldInvert)
        position = (short)(-position - 1);
      return position;
    }
  }
}