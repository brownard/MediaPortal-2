using Emulators.LibRetro.Controllers;
using Emulators.LibRetro.Controllers.Hid;
using Emulators.LibRetro.Controllers.Mapping;
using Emulators.LibRetro.Controllers.XInput;
using Emulators.LibRetro.GLContexts;
using Emulators.LibRetro.SoundProviders;
using Emulators.LibRetro.VideoProviders;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
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

namespace Emulators.LibRetro
{
  public class LibRetroFrontend : IDisposable
  {
    #region Logger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region Protected Members
    protected const int AUTO_SAVE_INTERVAL = 10 * 1000;
    protected readonly object _surfaceLock = new object();
    protected readonly object _audioLock = new object();
    protected LibRetroEmulator _retroEmulator;
    protected LibRetroSaveStateHandler _saveHandler;
    protected string _corePath;
    protected string _gamePath;
    protected string _saveDirectory;
    protected ITextureProvider _textureProvider;
    protected ISoundOutput _soundOutput;
    protected Thread _renderThread;
    protected volatile bool _doRender;
    protected double _vsync;
    protected bool _initialised;
    protected bool _syncToAudio = true;
    protected bool _autoSave = true;
    protected RenderDlgt _renderDlgt;
    protected bool _isPaused;
    protected ManualResetEventSlim _pauseWaitHandle;
    protected ManualResetEventSlim _isPausedHandle;
    HidListener _hidListener;
    #endregion

    #region Ctor
    public LibRetroFrontend(string corePath, string gamePath, string saveDirectory)
    {
      _corePath = corePath;
      _gamePath = gamePath;
      _saveDirectory = saveDirectory;
      _pauseWaitHandle = new ManualResetEventSlim(true);
      _isPausedHandle = new ManualResetEventSlim(false);
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

    public bool SyncToAudio
    {
      get { return _syncToAudio; }
      set { _syncToAudio = value; }
    }

    #endregion

    #region Public Methods
    public bool Init()
    {
      _hidListener = new HidListener();
      _hidListener.Register(SkinContext.Form.Handle);
      _retroEmulator = new LibRetroEmulator(_corePath)
      {
        SaveDirectory = _saveDirectory,
        LogDelegate = RetroLogDlgt,
        Controller = new HidGameControl(_hidListener, XBox360HidMapping.DEFAULT_MAPPING), //new XInputController(XInputMapper.GetDefaultMapping(false)),
        GLContext = new RetroGLContextProvider()
      };
      SetCoreVariables();
      _retroEmulator.VideoReady += OnVideoReady;
      _retroEmulator.FrameBufferReady += OnFrameBufferReady;
      _retroEmulator.AudioReady += OnAudioReady;
      _retroEmulator.Init();
      if (!_retroEmulator.LoadGame(_gamePath, _retroEmulator.SystemInfo.NeedsFullPath ? null : File.ReadAllBytes(_gamePath)))
        return false;
      _vsync = 1 / _retroEmulator.TimingInfo.VSyncRate;
      _textureProvider = new LibRetroTextureWrapper();
      _soundOutput = new LibRetroDirectSound();
      if (!_soundOutput.Init(SkinContext.Form.Handle, (int)_retroEmulator.TimingInfo.SampleRate))
      {
        _soundOutput.Dispose();
        _soundOutput = null;
      }
      _saveHandler = new LibRetroSaveStateHandler(_retroEmulator, _gamePath, _saveDirectory, AUTO_SAVE_INTERVAL);
      _saveHandler.LoadSaveRam();
      _initialised = true;
      return true;
    }

    public void Run()
    {
      _soundOutput.Play();
      _doRender = true;
      _renderThread = new Thread(DoRender);
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
      lock (_surfaceLock)
        _initialised = true;
    }

    public void ReleaseGUIResources()
    {
      lock (_surfaceLock)
      {
        _initialised = false;
        if (_textureProvider != null)
          _textureProvider.Release();
      }
    }

    public VideoInfo GetVideoInfo()
    {
      LibRetroEmulator emulator = _retroEmulator;
      return emulator != null ? emulator.VideoInfo : null;
    }
    #endregion

    #region Protected Methods
    protected void SetCoreVariables()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      CoreSetting coreSetting;
      if (!sm.Load<LibRetroCoreSettings>().TryGetCoreSetting(_corePath, out coreSetting) || coreSetting.Variables == null)
        return;
      foreach (VariableDescription variable in coreSetting.Variables)
        _retroEmulator.Variables.AddOrUpdate(variable);
    }

    protected void DoRender()
    {
      long timestamp = Stopwatch.GetTimestamp();
      while (_doRender)
      {
        if (_syncToAudio || NeedsRender(ref timestamp))
          _retroEmulator.Run();
        RenderFrame();
        if (_autoSave)
          _saveHandler.AutoSave();
        if (!_pauseWaitHandle.IsSet)
        {
          _isPausedHandle.Set();
          _pauseWaitHandle.Wait();
          _isPausedHandle.Reset();
        }
      }
      _saveHandler.SaveSaveRam();
      _retroEmulator.Dispose();
      _retroEmulator = null;
    }

    protected bool NeedsRender(ref long lastTimestamp)
    {
      long currentTimestamp = Stopwatch.GetTimestamp();
      double deltaMs = (double)(currentTimestamp - lastTimestamp) / Stopwatch.Frequency;
      if (deltaMs < _vsync)
        return false;
      lastTimestamp = currentTimestamp;
      return true;
    }

    protected void OnPausedChanged()
    {
      if (_isPaused)
      {
        if (_pauseWaitHandle != null)
          _pauseWaitHandle.Reset();
        _isPausedHandle.Wait();
        if (_soundOutput != null)
          _soundOutput.Pause();
      }
      else
      {
        _isPausedHandle.Wait();
        if (_soundOutput != null)
          _soundOutput.UnPause();
        if (_pauseWaitHandle != null)
          _pauseWaitHandle.Set();
      }
    }

    protected void RenderFrame()
    {
      RenderDlgt dlgt = _renderDlgt;
      if (dlgt != null)
        dlgt();
    }

    protected void OnVideoReady(object sender, EventArgs e)
    {
      lock (_surfaceLock)
        if (_initialised)
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight);
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      lock (_surfaceLock)
        if (_initialised)
        {
          int width = _retroEmulator.VideoInfo.BufferWidth;
          int height = _retroEmulator.VideoInfo.BufferHeight;
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.GLContext.Pixels, width, height, _retroEmulator.GLContext.BottomLeftOrigin);
        }
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.WriteSamples(_retroEmulator.AudioBuffer.Data, _retroEmulator.AudioBuffer.Length, _syncToAudio);
    }

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
      if (_hidListener != null)
      {
        _hidListener.Dispose();
        _hidListener = null;
      }
      if (_pauseWaitHandle != null)
      {
        _pauseWaitHandle.Dispose();
        _pauseWaitHandle = null;
      }
      if (_isPausedHandle != null)
      {
        _isPausedHandle.Dispose();
        _isPausedHandle = null;
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
    }
    #endregion
  }
}