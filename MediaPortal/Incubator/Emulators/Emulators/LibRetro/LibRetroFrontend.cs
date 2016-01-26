﻿using Emulators.LibRetro.Controllers;
using Emulators.LibRetro.Controllers.Hid;
using Emulators.LibRetro.Controllers.Mapping;
using Emulators.LibRetro.Controllers.XInput;
using Emulators.LibRetro.GLContexts;
using Emulators.LibRetro.Settings;
using Emulators.LibRetro.SoundProviders;
using Emulators.LibRetro.VideoProviders;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct3D9;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emulators.LibRetro.Render;

namespace Emulators.LibRetro
{
  public class LibRetroFrontend : IDisposable
  {
    #region ILogger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region Consts
    protected const int AUTO_SAVE_INTERVAL = 10 * 1000;
    #endregion

    #region Protected Members
    protected readonly object _surfaceLock = new object();
    protected readonly object _audioLock = new object();
    protected ManualResetEventSlim _pauseWaitHandle;

    protected LibRetroSettings _settings;
    protected LibRetroEmulator _retroEmulator;
    protected LibRetroSaveStateHandler _saveHandler;
    protected ITextureProvider _textureProvider;
    protected ISoundOutput _soundOutput;
    protected ControllerWrapper _controllerWrapper;
    protected SynchronisationStrategy _synchronisationStrategy;

    protected Thread _renderThread;
    protected volatile bool _doRender;
    protected RenderDlgt _renderDlgt;

    protected bool _libretroInitialized;
    protected bool _guiInitialized;
    protected bool _isPaused;

    protected string _corePath;
    protected string _gamePath;
    protected string _saveDirectory;
    protected string _saveName;

    protected bool _syncToAudio;
    protected bool _autoSave;
    #endregion

    #region Ctor
    public LibRetroFrontend(string corePath, string gamePath, string saveDirectory, string saveName)
    {
      _corePath = corePath;
      _gamePath = gamePath;
      _saveDirectory = saveDirectory;
      _saveName = saveName;
      _pauseWaitHandle = new ManualResetEventSlim(true);
      _guiInitialized = true;
    }
    #endregion

    #region Public Properties
    public object SurfaceLock
    {
      get { return _surfaceLock; }
    }

    public bool Paused
    {
      get { return _isPaused; }
    }

    public Texture Texture
    {
      get
      {
        lock (_surfaceLock)
          return _textureProvider != null ? _textureProvider.Texture : null;
      }
    }
    #endregion

    #region Public Methods
    public bool Init()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
      try
      {
        InitializeLibRetro();
        if (!LoadGame())
          return false;
        InitializeOutputs();
        InitializeSaveStateHandler();
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroFrontend: Error initialising Libretro core: {0}", ex);
        return false;
      }
      _libretroInitialized = true;
      return true;
    }

    public void Run()
    {
      if (!_libretroInitialized)
        return;
      _controllerWrapper.Start();
      _doRender = true;
      _renderThread = new Thread(DoRender) { Name = "LibRetroRenderThread" };
      _renderThread.Start();
    }

    public void Pause()
    {
      if (_isPaused)
        return;
      _isPaused = true;
      OnPausedChanged();
    }

    public void Unpause()
    {
      if (!_isPaused)
        return;
      _isPaused = false;
      OnPausedChanged();
    }

    protected void OnPausedChanged()
    {
      if (_pauseWaitHandle == null)
        return;

      if (_isPaused)
        _pauseWaitHandle.Reset();
      else
        _pauseWaitHandle.Set();
    }

    public void SetVolume(int volume)
    {
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.SetVolume(volume);
    }

    public bool SetRenderDelegate(RenderDlgt dlgt)
    {
      _renderDlgt = dlgt;
      return true;
    }

    public void ReallocGUIResources()
    {
      if (_synchronisationStrategy != null)
        _synchronisationStrategy.Update();
      lock (_surfaceLock)
        _guiInitialized = true;
    }

    public void ReleaseGUIResources()
    {
      lock (_surfaceLock)
      {
        _guiInitialized = false;
        if (_textureProvider != null)
          _textureProvider.Release();
      }
    }

    public VideoInfo GetVideoInfo()
    {
      LibRetroEmulator emulator = _retroEmulator;
      return emulator != null ? emulator.VideoInfo : null;
    }

    public TimingInfo GetTimingInfo()
    {
      LibRetroEmulator emulator = _retroEmulator;
      return emulator != null ? emulator.TimingInfo : null;
    }
    #endregion

    #region Init
    protected void InitializeLibRetro()
    {
      CreateControllerWrapper();
      _retroEmulator = new LibRetroEmulator(_corePath)
      {
        SaveDirectory = _saveDirectory,
        LogDelegate = RetroLogDlgt,
        Controller = _controllerWrapper,
        GLContext = new RetroGLContextProvider()
      };

      SetCoreVariables();
      _retroEmulator.VideoReady += OnVideoReady;
      _retroEmulator.FrameBufferReady += OnFrameBufferReady;
      _retroEmulator.AudioReady += OnAudioReady;
      _retroEmulator.Init();
    }

    protected void InitializeOutputs()
    {
      _textureProvider = new LibRetroTextureWrapper();
      InitializeAudio();
      if (!_syncToAudio)
        _synchronisationStrategy = new SynchronisationStrategy(_retroEmulator.TimingInfo.VSyncRate, _settings.EnableVSync);
    }

    protected void InitializeAudio()
    {
      VideoSettings videoSettings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      Guid audioRenderer;
      if (videoSettings == null || videoSettings.AudioRenderer == null || !Guid.TryParse(videoSettings.AudioRenderer.CLSID, out audioRenderer))
        audioRenderer = Guid.Empty;

      _syncToAudio = _settings.SyncToAudio;
      _soundOutput = new LibRetroDirectSound();
      if (!_soundOutput.Init(SkinContext.Form.Handle, audioRenderer, (int)_retroEmulator.TimingInfo.SampleRate, _settings.AudioBufferSize))
      {
        _soundOutput.Dispose();
        _soundOutput = null;
        _syncToAudio = false;
      }
    }

    protected void InitializeSaveStateHandler()
    {
      _autoSave = _settings.AutoSave;
      _saveHandler = new LibRetroSaveStateHandler(_retroEmulator, _saveName, _saveDirectory, _settings.AutoSaveInterval);
      _saveHandler.LoadSaveRam();
    }

    protected void SetCoreVariables()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      CoreSetting coreSetting;
      if (!sm.Load<LibRetroCoreSettings>().TryGetCoreSetting(_corePath, out coreSetting) || coreSetting.Variables == null)
        return;
      foreach (VariableDescription variable in coreSetting.Variables)
        _retroEmulator.Variables.AddOrUpdate(variable);
    }

    protected void CreateControllerWrapper()
    {
      _controllerWrapper = new ControllerWrapper(_settings.MaxPlayers);
      DeviceProxy deviceProxy = new DeviceProxy();
      List<IMappableDevice> deviceList = deviceProxy.GetDevices(false);
      MappingProxy mappingProxy = new MappingProxy();

      foreach (PortMapping port in mappingProxy.PortMappings)
      {
        IMappableDevice device = deviceProxy.GetDevice(port.DeviceId, port.SubDeviceId, deviceList);
        if (device != null)
        {
          RetroPadMapping mapping = mappingProxy.GetDeviceMapping(device);
          device.Map(mapping);
          _controllerWrapper.AddController(device, port.Port);
        }
      }
    }

    protected bool LoadGame()
    {
      byte[] gameData = _retroEmulator.SystemInfo.NeedsFullPath ? null : File.ReadAllBytes(_gamePath);
      return _retroEmulator.LoadGame(_gamePath, gameData);
    }
    #endregion

    #region Render Thread
    protected void DoRender()
    {
      try
      {
        _soundOutput.Play();
        while (_doRender)
        {
          if (_synchronisationStrategy == null || !_synchronisationStrategy.ShouldDuplicate)
            RunEmulator();
          RenderFrame();
          CheckPauseState();
        }
        OnRenderThreadFinished();
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroFrontend: Error in DoRender: {0}", ex);
      }
      finally
      {
        _retroEmulator.Dispose();
        _retroEmulator = null;
      }
    }

    protected void RunEmulator()
    {
      _retroEmulator.Run();
      if (_autoSave)
        _saveHandler.AutoSave();
    }

    protected void RenderFrame()
    {
      RenderDlgt dlgt = _renderDlgt;
      if (dlgt != null)
      {
        if (_synchronisationStrategy != null)
          _synchronisationStrategy.Synchronise(false);
        dlgt();
      }
      else if (_synchronisationStrategy != null)
      {
        _synchronisationStrategy.Synchronise(true);
      }
    }

    protected void CheckPauseState()
    {
      if (!_pauseWaitHandle.IsSet)
      {
        OnRenderThreadPaused();
        _pauseWaitHandle.Wait();
        OnRenderThreadUnPaused();
      }
    }

    protected void OnRenderThreadPaused()
    {
      lock (_audioLock)
      {
        if (_soundOutput != null)
          _soundOutput.Pause();
      }
    }

    protected void OnRenderThreadUnPaused()
    {
      lock (_audioLock)
      {
        if (_soundOutput != null)
          _soundOutput.UnPause();
      }
    }

    protected void OnRenderThreadFinished()
    {
      _saveHandler.SaveSaveRam();
      _retroEmulator.UnloadGame();
      _retroEmulator.DeInit();
    }
    #endregion

    #region Audio/Video Output
    protected void UpdateVideo()
    {
      lock (_surfaceLock)
      {
        if (_guiInitialized)
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight, false);
      }
    }

    protected void UpdateFrameBuffer()
    {
      lock (_surfaceLock)
      {
        if (_guiInitialized)
        {
          int width = _retroEmulator.VideoInfo.BufferWidth;
          int height = _retroEmulator.VideoInfo.BufferHeight;
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.GLContext.Pixels, width, height, _retroEmulator.GLContext.BottomLeftOrigin);
        }
      }
    }

    protected void UpdateAudio()
    {
      lock (_audioLock)
      {
        if (_soundOutput != null)
          _soundOutput.WriteSamples(_retroEmulator.AudioBuffer.Data, _retroEmulator.AudioBuffer.Length, _syncToAudio);
      }
    }

    protected void OnVideoReady(object sender, EventArgs e)
    {
      UpdateVideo();
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      UpdateFrameBuffer();
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
      UpdateAudio();
    }
    #endregion

    #region LibRetro Logging
    protected void RetroLogDlgt(LibRetroCore.RETRO_LOG_LEVEL level, string message)
    {
      string format = "LibRetro: {0}";
      switch (level)
      {
        case LibRetroCore.RETRO_LOG_LEVEL.INFO:
          Logger.Info(format, message);
          break;
        case LibRetroCore.RETRO_LOG_LEVEL.DEBUG:
          Logger.Debug(format, message);
          break;
        case LibRetroCore.RETRO_LOG_LEVEL.WARN:
          Logger.Warn(format, message);
          break;
        case LibRetroCore.RETRO_LOG_LEVEL.ERROR:
          Logger.Error(format, message);
          break;
        default:
          Logger.Debug(format, message);
          break;
      }
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
      _doRender = false;
      if (_pauseWaitHandle != null && !_pauseWaitHandle.IsSet)
        _pauseWaitHandle.Set();

      if (_renderThread != null)
      {
        _renderThread.Join();
        _renderThread = null;
      }
      if (_controllerWrapper != null)
      {
        _controllerWrapper.Dispose();
        _controllerWrapper = null;
      }
      if (_pauseWaitHandle != null)
      {
        _pauseWaitHandle.Dispose();
        _pauseWaitHandle = null;
      }
      if (_textureProvider != null)
      {
        _textureProvider.Dispose();
        _textureProvider = null;
      }
      if (_soundOutput != null)
      {
        _soundOutput.Dispose();
        _soundOutput = null;
      }
      if (_synchronisationStrategy != null)
      {
        _synchronisationStrategy.Stop();
        _synchronisationStrategy = null;
      }
    }
    #endregion
  }
}