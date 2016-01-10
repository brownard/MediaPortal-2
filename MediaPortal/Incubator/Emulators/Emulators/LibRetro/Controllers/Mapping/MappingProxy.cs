using Emulators.LibRetro.Controllers.Hid;
using Emulators.LibRetro.Controllers.XInput;
using Emulators.LibRetro.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using SharpDX.XInput;
using SharpLib.Hid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.Mapping
{
  public class MappingProxy
  {
    LibRetroMappingSettings _settings;

    public MappingProxy()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      _settings = sm.Load<LibRetroMappingSettings>();
      _settings.Ports.Sort((p1, p2) => p1.Port.CompareTo(p2.Port));
    }

    public List<PortMapping> PortMappings
    {
      get { return _settings.Ports; }
    }

    public List<RetroPadMapping> DeviceMappings
    {
      get { return _settings.Mappings; }
    }

    public PortMapping GetPortMapping(int port)
    {
      if (_settings.Ports.Count < port + 1)
        return new PortMapping() { Port = port };
      return _settings.Ports[port];
    }

    public void AddPortMapping(PortMapping portMapping)
    {
      if (_settings.Ports.Count < portMapping.Port + 1)
        _settings.Ports.Add(portMapping);
      else
        _settings.Ports[portMapping.Port] = portMapping;
    }

    public RetroPadMapping GetDeviceMapping(IMappableDevice device)
    {
      RetroPadMapping mapping = _settings.Mappings.FirstOrDefault(m => m.DeviceId == device.DeviceId && m.SubDeviceId == device.SubDeviceId);
      if (mapping == null)
        mapping = device.DefaultMapping != null ? device.DefaultMapping : CreateNewMapping(device);
      return mapping;
    }

    public void AddDeviceMapping(RetroPadMapping deviceMapping)
    {
      _settings.Mappings.RemoveAll(m => m.DeviceId == deviceMapping.DeviceId && m.SubDeviceId == deviceMapping.SubDeviceId);
      _settings.Mappings.Add(deviceMapping);
    }

    public void Save()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      sm.Save(_settings);
    }

    protected RetroPadMapping CreateNewMapping(IMappableDevice device)
    {
      return new RetroPadMapping()
      {
        DeviceId = device.DeviceId,
        SubDeviceId = device.SubDeviceId,
        DeviceName = device.DeviceName
      };
    }
  }
}
