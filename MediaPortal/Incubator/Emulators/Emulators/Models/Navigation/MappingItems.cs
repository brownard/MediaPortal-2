﻿using Emulators.LibRetro.Controllers.Mapping;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models.Navigation
{
  public class PortMappingItem : ListItem
  {
    protected PortMapping _port;

    public PortMapping PortMapping
    {
      get { return _port; }
    }

    public PortMappingItem(string name, PortMapping port)
      : base(Consts.KEY_NAME, name)
    {
      _port = port;
    }
  }

  public class MappableDeviceItem : ListItem
  {
    protected IMappableDevice _device;

    public IMappableDevice Device
    {
      get { return _device; }
    }

    public MappableDeviceItem(string name, IMappableDevice device)
      : base(Consts.KEY_NAME, name)
    {
      _device = device;
    }
  }

  public class MappedInputItem : ListItem
  {
    public const string KEY_MAPPED_INPUT = "MappedInput";
    protected MappedInput _mappedInput;

    public MappedInput MappedInput
    {
      get { return _mappedInput; }
    }

    public MappedInputItem(string name, MappedInput mappedInput)
      : base(Consts.KEY_NAME, name)
    {
      _mappedInput = mappedInput;
    }

    public void Update(DeviceInput deviceInput)
    {
      _mappedInput.Input = deviceInput;
      string label = deviceInput != null ? deviceInput.Label : "";
      SetLabel(KEY_MAPPED_INPUT, label);
    }
  }
}
