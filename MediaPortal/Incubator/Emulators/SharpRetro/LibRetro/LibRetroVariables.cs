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

    public List<VariableDescription> GetAllVariables()
    {
      return _variables.Values.ToList();
    }

    public bool Contains(string variableName)
    {
      return _variables.ContainsKey(variableName);
    }

    public void AddOrUpdate(VariableDescription variable)
    {
      VariableDescription vd;
      if (_variables.TryGetValue(variable.Name, out vd))
        variable.SelectedOption = vd.SelectedOption;
      _variables[variable.Name] = variable;
      _updated = true;
    }

    public void AddOrUpdate(string variableName, string selectedOption)
    {
      VariableDescription vd;
      if (_variables.TryGetValue(variableName, out vd))
        vd.SelectedOption = selectedOption;
      else
        _variables[variableName] = new VariableDescription() { Name = variableName, SelectedOption = selectedOption };
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
    protected string _selectedOption;

    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Options { get; set; }

    public string DefaultOption
    {
      get { return Options != null && Options.Length > 0 ? Options[0] : ""; }
    }

    public string SelectedOption
    {
      get { return string.IsNullOrEmpty(_selectedOption) ? DefaultOption : _selectedOption; }
      set { _selectedOption = value; }
    }

    public override string ToString()
    {
      return string.Format("{0} ({1}) = ({2})", Name, Description, string.Join("|", Options));
    }
  }
}