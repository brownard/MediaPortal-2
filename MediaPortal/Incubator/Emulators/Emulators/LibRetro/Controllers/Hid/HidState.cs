using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Hid
{
  class HidState
  {
    public HidState(string name, string friendlyName, HashSet<ushort> buttons, Dictionary<ushort, HidAxisState> axisStates, SharpLib.Hid.DirectionPadState directionPadState)
    {
      Name = name;
      FriendlyName = friendlyName;
      Buttons = buttons;
      AxisStates = axisStates;
      DirectionPadState = directionPadState;
    }
    public string Name { get; private set; }
    public string FriendlyName { get; private set; }
    public HashSet<ushort> Buttons { get; private set; }
    public Dictionary<ushort, HidAxisState> AxisStates { get; private set; }
    public SharpLib.Hid.DirectionPadState DirectionPadState { get; private set; }
  }

  class HidAxisState
  {
    public HidAxisState(string name, ushort id, uint value, ushort bitSize)
    {
      Name = name;
      Id = id;
      Value = value;
      BitSize = bitSize;
    }
    public string Name { get; private set; }
    public ushort Id { get; private set; }
    public uint Value { get; private set; }
    public ushort BitSize { get; private set; }
  }
}
