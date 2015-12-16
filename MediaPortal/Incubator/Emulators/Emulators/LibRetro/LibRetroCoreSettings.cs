using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroCoreSettings
  {
    protected List<CoreOptions> _coreOptions;
    public List<CoreOptions> CoreOptions
    {
      get
      {
        if (_coreOptions == null)
          _coreOptions = new List<CoreOptions>();
        return _coreOptions;
      }
    }
  }

  public class CoreOptions
  {
    protected List<VariableDescription> _variables;
    public string CorePath { get; set; }
    public List<VariableDescription> Variables
    {
      get
      {
        if (_variables == null)
          _variables = new List<VariableDescription>();
        return _variables;
      }
    }
  }
}