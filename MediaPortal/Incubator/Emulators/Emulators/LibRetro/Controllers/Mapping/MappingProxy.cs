using Emulators.LibRetro.Controllers.XInput;
using Emulators.LibRetro.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using SharpDX.XInput;
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
    protected static readonly UserIndex[] XINPUT_USER_INDEXES = new[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };
    LibRetroMappingSettings _settings;

    public MappingProxy()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      _settings = sm.Load<LibRetroMappingSettings>();
      _settings.Ports.Sort((p1, p2) => p1.Port.CompareTo(p2.Port));
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

    public List<IMappableDevice> GetDevices(bool connectedOnly)
    {
      List<IMappableDevice> deviceList = new List<IMappableDevice>();
      AddXInputDevices(deviceList, connectedOnly);
      AddHidDevices(deviceList);
      return deviceList;
    }

    public IMappableDevice GetDevice(Guid deviceId, string subDeviceId)
    {
      if (deviceId == Guid.Empty)
        return null;
      return GetDevices(false).FirstOrDefault(d => d.DeviceId == deviceId && d.SubDeviceId == subDeviceId);
    }

    public ControllerWrapper CreateControllers()
    {
      List<IMappableDevice> deviceList = GetDevices(false);
      ControllerWrapper controllerWrapper = new ControllerWrapper();
      foreach (PortMapping port in _settings.Ports)
      {
        IMappableDevice device = deviceList.FirstOrDefault(d => d.DeviceId == port.DeviceId && d.SubDeviceId == port.SubDeviceId);
        if (device != null)
        {
          RetroPadMapping mapping = _settings.Mappings.FirstOrDefault(m => m.DeviceId == port.DeviceId && m.SubDeviceId == port.SubDeviceId);
          device.Map(mapping);
          controllerWrapper.AddController(device, port.Port);
        }
      }
      return controllerWrapper;
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

    protected void AddXInputDevices(List<IMappableDevice> deviceList, bool connectedOnly)
    {
      foreach (UserIndex userIndex in XINPUT_USER_INDEXES)
      {
        XInputController controller = new XInputController(userIndex);
        if (!connectedOnly || controller.IsConnected())
          deviceList.Add(controller);
      }
    }

    protected void AddHidDevices(List<IMappableDevice> deviceList)
    {

    }
  }
}
