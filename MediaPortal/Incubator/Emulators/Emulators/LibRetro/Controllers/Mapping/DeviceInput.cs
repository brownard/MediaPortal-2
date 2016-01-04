using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  enum InputType
  {
    Axis,
    Button
  }

  class DeviceInput
  {
    protected string _label;
    protected string _id;
    protected InputType _inputType;
    protected bool _positiveValues;

    public DeviceInput(string label, string id, InputType inputType, bool positiveValues = true)
    {
      _label = label;
      _id = id;
      _inputType = inputType;
      _positiveValues = positiveValues;
    }

    public string Label
    {
      get { return _label; }
    }

    public string Id
    {
      get { return _id; }
    }

    public InputType InputType
    {
      get { return _inputType; }
    }

    public bool PositiveValues
    {
      get { return _positiveValues; }
    }
  }
}
