﻿using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using Emulators.LibRetro.Controllers.Mapping;
using MediaPortal.Common;
using System.Threading;
using Emulators.Models.Navigation;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Common.Localization;
using Emulators.LibRetro.Controllers;
using MediaPortal.Common.Services.Settings;
using Emulators.LibRetro.Settings;

namespace Emulators.Models
{
  public class LibRetroMappingModel : IWorkflowModel
  {
    #region Consts
    public static readonly Guid MODEL_ID = new Guid("FD908F54-B37E-408F-9D53-EB72A2912617");
    public static readonly Guid STATE_PORT_SELECT = new Guid("02FEF896-7C0E-444D-9657-44660E0ED90A");
    public static readonly Guid STATE_DEVICE_CONFIGURE = new Guid("499FFA69-A92A-4BC8-9F87-0A578A62815A");
    public static readonly Guid STATE_MAP_INPUT = new Guid("AAFD9AD5-F31D-41FE-A717-D9D7801EC27F");
    public static string DIALOG_CHOOSE_DEVICE = "dialog_mapping_devices";
    protected const int POLL_INITIAL_WAIT = 500;
    protected const int POLL_INTERVAL = 100;
    #endregion

    #region Protected Members
    protected AbstractProperty _currentPlayerHeaderProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _currentDeviceNameProperty = new WProperty(typeof(string), null);
    protected SettingsChangeWatcher<LibRetroSettings> _settings;

    protected int _maxPlayers;
    protected ItemsList _portsList = new ItemsList();
    protected ItemsList _deviceList = new ItemsList();
    protected ItemsList _inputList = new ItemsList();
    protected DeviceProxy _deviceProxy = new DeviceProxy();
    protected MappingProxy _mappingProxy = new MappingProxy();
    protected PortMapping _currentPortMapping;
    protected IMappableDevice _currentDevice;
    protected IDeviceMapper _deviceMapper;
    protected RetroPadMapping _currentMapping;
    protected MappedInput _mappedInput;
    protected Thread _inputPollThread;
    protected volatile bool _doPoll;
    #endregion

    #region Constructor
    public LibRetroMappingModel()
    {
      _settings = new SettingsChangeWatcher<LibRetroSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
      InitSettings();
    }
    #endregion

    #region Public Properties
    public ItemsList Ports
    {
      get { return _portsList; }
    }

    public ItemsList Devices
    {
      get { return _deviceList; }
    }

    public ItemsList Inputs
    {
      get { return _inputList; }
    }

    public AbstractProperty CurrentPlayerHeaderProperty
    {
      get { return _currentPlayerHeaderProperty; }
    }

    public string CurrentPlayerHeader
    {
      get { return (string)_currentPlayerHeaderProperty.GetValue(); }
      set { _currentPlayerHeaderProperty.SetValue(value); }
    }

    public AbstractProperty CurrentDeviceNameProperty
    {
      get { return _currentDeviceNameProperty; }
    }

    public string CurrentDeviceName
    {
      get { return (string)_currentDeviceNameProperty.GetValue(); }
      set { _currentDeviceNameProperty.SetValue(value); }
    }
    #endregion

    #region Public Methods
    public void PortItemSelected(PortMappingItem item)
    {
      _currentPortMapping = item.PortMapping;
      _currentDevice = _deviceProxy.GetDevice(_currentPortMapping.DeviceId, _currentPortMapping.SubDeviceId);
      CurrentDeviceName = _currentDevice != null ? _currentDevice.DeviceName : null;
      CurrentPlayerHeader = item.Label(Consts.KEY_NAME, "").Evaluate();
      var wm = ServiceRegistration.Get<IWorkflowManager>();
      wm.NavigatePushAsync(STATE_DEVICE_CONFIGURE, new NavigationContextConfig()
      {
        NavigationContextDisplayLabel = CurrentPlayerHeader
      });
    }

    public void ShowDeviceDialog()
    {
      UpdateDeviceList();
      ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_CHOOSE_DEVICE);
    }

    public void DeviceItemSelected(MappableDeviceItem item)
    {
      _currentDevice = item.Device;
      CurrentDeviceName = _currentDevice != null ? _currentDevice.DeviceName : null;
      UpdateMappings();
    }

    public void InputItemSelected(MappedInputItem item)
    {
      _mappedInput = item.MappedInput;
      if (_mappedInput != null)
      {
        var wm = ServiceRegistration.Get<IWorkflowManager>();
        wm.NavigatePushAsync(STATE_MAP_INPUT);
      }
    }
    #endregion

    #region Protected Methods
    protected void OnSettingsChanged(object sender, EventArgs e)
    {
      InitSettings();
    }

    protected void InitSettings()
    {      
      _maxPlayers = _settings.Settings.MaxPlayers;
      if (_maxPlayers < 1)
        _maxPlayers = 1;
    }

    protected void UpdateState(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      if (oldContext.WorkflowState.StateId == STATE_MAP_INPUT)
      {
        EndMapping();
        UpdateInputList();
      }

      Guid newState = newContext.WorkflowState.StateId;
      if (newState == STATE_PORT_SELECT)
      {
        if (!push)
          Save();
        Reset();
        UpdatePortsList();
      }
      if (newState == STATE_DEVICE_CONFIGURE)
      {
        if (push)
          UpdateMappings();
      }
      else if (newState == STATE_MAP_INPUT)
      {
        BeginMapping(newContext);
      }
    }

    protected void Save()
    {
      if (_currentPortMapping != null)
      {
        if (_currentDevice != null)
        {
          _currentPortMapping.SetDevice(_currentDevice);
          _mappingProxy.AddPortMapping(_currentPortMapping);
        }
        else
        {
          _mappingProxy.RemovePortMapping(_currentPortMapping.Port);
        }
      }

      if (_currentMapping != null)
        _mappingProxy.AddDeviceMapping(_currentMapping);

      if (_currentPortMapping != null || _currentMapping != null)
        _mappingProxy.Save();
    }

    protected void Reset()
    {
      EndMapping();
      DisposeDeviceMapper();
      CurrentPlayerHeader = null;
      CurrentDeviceName = null;
      _currentDevice = null;
      _currentPortMapping = null;
      _currentMapping = null;
      _mappedInput = null;
      _portsList.Clear();
      _portsList.FireChange();
      _deviceList.Clear();
      _deviceList.FireChange();
      _inputList.Clear();
      _inputList.FireChange();
    }

    protected void UpdatePortsList()
    {
      _portsList.Clear();
      for (int i = 0; i < _maxPlayers; i++)
      {
        PortMappingItem portItem = new PortMappingItem(LocalizationHelper.Translate("[Emulators.LibRetro.PlayerNumber]", i + 1), _mappingProxy.GetPortMapping(i));
        portItem.Command = new MethodDelegateCommand(() => PortItemSelected(portItem));
        _portsList.Add(portItem);
      }
      _portsList.FireChange();
    }

    protected void UpdateDeviceList()
    {
      _deviceList.Clear();
      MappableDeviceItem noDeviceItem = new MappableDeviceItem("[Emulators.LibRetro.InputDevice.None]", null);
      _deviceList.Add(noDeviceItem);
      foreach (IMappableDevice device in _deviceProxy.GetDevices(true))
      {
        MappableDeviceItem deviceItem = new MappableDeviceItem(device.DeviceName, device);
        _deviceList.Add(deviceItem);
      }
      _deviceList.FireChange();
    }

    protected void UpdateMappings()
    {
      DisposeDeviceMapper();
      _currentMapping = null;
      if (_currentDevice != null)
      {
        _deviceMapper = _currentDevice.CreateMapper();
        _currentMapping = _mappingProxy.GetDeviceMapping(_currentDevice);
      }
      RecreateInputList();
    }

    protected void RecreateInputList()
    {
      _inputList.Clear();
      if (_currentMapping != null)
      {
        foreach (MappedInput mappedInput in _currentMapping.AvailableInputs)
        {
          DeviceInput input;
          if ((mappedInput.Button.HasValue && _currentMapping.ButtonMappings.TryGetValue(mappedInput.Button.Value, out input))
            || (mappedInput.Analog.HasValue && _currentMapping.AnalogMappings.TryGetValue(mappedInput.Analog.Value, out input)))
            mappedInput.Input = input;

          MappedInputItem inputItem = new MappedInputItem(mappedInput.Name, mappedInput);
          inputItem.Command = new MethodDelegateCommand(() => InputItemSelected(inputItem));
          _inputList.Add(inputItem);
        }
      }
      _inputList.FireChange();
    }

    protected void UpdateInputList()
    {
      foreach (MappedInputItem item in _inputList)
        item.Update();
      _inputList.FireChange();
    }

    protected void BeginMapping(NavigationContext newContext)
    {
      if (_deviceMapper == null || _currentMapping == null || _mappedInput == null)
        return;
      _doPoll = true;
      _inputPollThread = new Thread(() => DoPoll(_deviceMapper, _currentMapping, _mappedInput)) { IsBackground = true };
      _inputPollThread.Start();
    }

    protected void EndMapping()
    {
      _doPoll = false;
      if (_inputPollThread != null)
      {
        _inputPollThread.Join();
        _inputPollThread = null;
      }
    }

    protected void DoPoll(IDeviceMapper mapper, RetroPadMapping currentMapping, MappedInput mappedInput)
    {
      Thread.Sleep(POLL_INITIAL_WAIT);
      while (_doPoll)
      {
        DeviceInput input = mapper.GetPressedInput();
        if (input != null)
        {
          mappedInput.Input = input;
          currentMapping.Map(mappedInput);
          var wm = ServiceRegistration.Get<IWorkflowManager>();
          wm.NavigatePopToStateAsync(STATE_DEVICE_CONFIGURE, false);
          break;
        }
        Thread.Sleep(POLL_INTERVAL);
      }
    }

    protected void DisposeDeviceMapper()
    {
      if (_deviceMapper == null)
        return;
      IDisposable disposable = _deviceMapper as IDisposable;
      if (disposable != null)
        disposable.Dispose();
      _deviceMapper = null;
    }
    #endregion

    #region IWorkflow
    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      UpdateState(oldContext, newContext, push);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UpdateState(oldContext, newContext, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UpdateState(oldContext, newContext, false);
      Save();
      Reset();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {

    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
    #endregion
  }
}