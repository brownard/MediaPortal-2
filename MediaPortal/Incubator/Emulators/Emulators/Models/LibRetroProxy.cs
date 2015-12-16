﻿using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class LibRetroProxy
  {
    protected string _corePath;
    protected LibRetroEmulator _retro;
    protected string _name;
    protected List<string> _extensions;
    protected List<VariableDescription> _options;

    public LibRetroProxy(string corePath)
    {
      _corePath = corePath;
    }

    public string Name
    {
      get { return _name; }
    }

    public List<string> Extensions
    {
      get { return _extensions; }
    }

    public List<VariableDescription> Options
    {
      get { return _options; }
    }

    public bool Init()
    {
      _retro = new LibRetroEmulator(_corePath);
      try
      {
        _retro.Init();
        InitializeProperties();
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroProxy: Exception initialising LibRetro core '{0}'", ex, _corePath);
      }
      finally
      {
        _retro.Dispose();
        _retro = null;
      }
      return false;
    }

    protected void InitializeProperties()
    {
      if (_retro == null)
        return;
      SystemInfo coreInfo = _retro.SystemInfo;
      _name = coreInfo.LibraryName;
      _extensions = coreInfo.ValidExtensions.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => "." + s.ToLowerInvariant()).ToList();
      _options = _retro.Variables.GetAllVariables();
    }
  }
}
