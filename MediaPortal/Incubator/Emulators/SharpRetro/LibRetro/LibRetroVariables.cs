using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class LibRetroVariables
  {
    protected Dictionary<string, VariableDescription> _variables = new Dictionary<string, VariableDescription>();
    protected bool _updated;

    public bool Updated
    {
      get { return _updated; }
    }

    public bool Contains(string variableName)
    {
      return _variables.ContainsKey(variableName);
    }

    public void AddOrUpdate(VariableDescription variable)
    {
      _variables[variable.Name] = variable;
      _updated = true;
    }

    public bool TryGet(string variableName, out VariableDescription variable)
    {
      _updated = false;
      return _variables.TryGetValue(variableName, out variable);
    }
  }

  public class VariableDescription
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Options { get; set; }
    public string DefaultOption { get { return Options[0]; } }

    public override string ToString()
    {
      return string.Format("{0} ({1}) = ({2})", Name, Description, string.Join("|", Options));
    }
  }
}
