using SharpRetro.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;
using Emulators.LibRetro.Controllers.Hid;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace Emulators.LibRetro.Controllers
{
  public class ControllerWrapper : IRetroPad, IRetroAnalog, IDisposable
  {
    #region Dummy Controller
    class DummyController : IRetroPad, IRetroAnalog
    {
      public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
      {
        return false;
      }
      public short GetAnalog(uint port, LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction)
      {
        return 0;
      }
    }
    #endregion

    protected const int MAX_CONTROLLERS = 8;
    protected IRetroPad[] _retroPads;
    protected IRetroAnalog[] _retroAnalogs;
    protected List<IHidDevice> _hidDevices;
    protected HidListener _hidListener;

    public ControllerWrapper()
    {
      _retroPads = new IRetroPad[MAX_CONTROLLERS];
      _retroAnalogs = new IRetroAnalog[MAX_CONTROLLERS];
      _hidDevices = new List<IHidDevice>(MAX_CONTROLLERS);

      DummyController dummy = new DummyController();
      for (int i = 0; i < MAX_CONTROLLERS; i++)
      {
        _retroPads[i] = dummy;
        _retroAnalogs[i] = dummy;
      }
    }

    public void AddController(IRetroController controller, int port)
    {
      if (port >= MAX_CONTROLLERS)
        return;

      IRetroPad retroPad = controller as IRetroPad;
      if (retroPad != null)
        _retroPads[port] = retroPad;

      IRetroAnalog retroAnalog = controller as IRetroAnalog;
      if (retroAnalog != null)
        _retroAnalogs[port] = retroAnalog;

      IHidDevice hidDevice = controller as IHidDevice;
      if (hidDevice != null)
        _hidDevices.Add(hidDevice);
    }

    public void Start()
    {
      if (_hidDevices.Count > 0)
      {
        _hidListener = new HidListener();
        _hidListener.StateChanged += HidListener_StateChanged;
        _hidListener.Register(SkinContext.Form.Handle);
      }
    }

    private void HidListener_StateChanged(object sender, HidStateEventArgs e)
    {
      foreach (IHidDevice device in _hidDevices)
        device.UpdateState(e.State);
    }

    public bool IsButtonPressed(uint port, LibRetroCore.RETRO_DEVICE_ID_JOYPAD button)
    {
      return port < MAX_CONTROLLERS ? _retroPads[port].IsButtonPressed(port, button) : false;
    }

    public short GetAnalog(uint port, LibRetroCore.RETRO_DEVICE_INDEX_ANALOG index, LibRetroCore.RETRO_DEVICE_ID_ANALOG direction)
    {
      return port < MAX_CONTROLLERS ? _retroAnalogs[port].GetAnalog(port, index, direction) : (short)0;
    }

    public void Dispose()
    {
      if (_hidListener != null)
      {
        _hidListener.Dispose();
        _hidListener = null;
      }
    }
  }
}