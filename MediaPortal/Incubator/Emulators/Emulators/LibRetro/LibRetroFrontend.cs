using Emulators.LibRetro.Controllers;
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

    #region Consts
    protected const int AUTO_SAVE_INTERVAL = 10 * 1000;
    #endregion

    #region Protected Members
    protected readonly object _surfaceLock = new object();
    protected readonly object _audioLock = new object();
    protected ManualResetEventSlim _pauseWaitHandle;

    protected LibRetroEmulator _retroEmulator;
    protected LibRetroSaveStateHandler _saveHandler;
    protected ITextureProvider _textureProvider;
    protected ISoundOutput _soundOutput;
    protected ControllerWrapper _controllerWrapper;

    protected Thread _renderThread;
    protected volatile bool _doRender;
    protected RenderDlgt _renderDlgt;

    protected bool _libretroInitialized;
    protected bool _guiInitialized;
    protected bool _isPaused;

    protected double _secondsPerFrame;
    protected long _lastRenderTimestamp;

    protected string _corePath;
    protected string _gamePath;
    protected string _saveDirectory;

    protected bool _syncToAudio;
    protected bool _autoSave;

    protected bool _videoReady;
    protected bool _frameBufferReady;
    protected bool _audioReady;
    #endregion

    #region Ctor
    public LibRetroFrontend(string corePath, string gamePath, string saveDirectory)
    {
      _corePath = corePath;
      _gamePath = gamePath;
      _saveDirectory = saveDirectory;
      _pauseWaitHandle = new ManualResetEventSlim(true);
      _syncToAudio = true;
      _autoSave = true;
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

    public bool SyncToAudio
    {
      get { return _syncToAudio; }
      set { _syncToAudio = value; }
    }

    #endregion

    #region Public Methods
    public bool Init()
    {
      InitializeLibRetro();
      if (!LoadGame())
        return false;
      InitializeOutputs();      
      _libretroInitialized = true;
      return true;
    }

    public void Run()
    {
      if (!_libretroInitialized)
        return;
      _controllerWrapper.Start();
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
    #endregion

    #region Protected Methods
    protected void InitializeLibRetro()
    {
      _controllerWrapper = new MappingProxy().CreateControllers();
      //_controllerWrapper.AddController(new HidGameControl(XBox360HidMapping.DEFAULT_MAPPING), 0);

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
      _secondsPerFrame = 1 / _retroEmulator.TimingInfo.VSyncRate;
      _textureProvider = new LibRetroTextureWrapper();
      _soundOutput = new LibRetroDirectSound();
      if (!_soundOutput.Init(SkinContext.Form.Handle, (int)_retroEmulator.TimingInfo.SampleRate))
      {
        _soundOutput.Dispose();
        _soundOutput = null;
        _syncToAudio = false;
      }
      _saveHandler = new LibRetroSaveStateHandler(_retroEmulator, _gamePath, _saveDirectory, AUTO_SAVE_INTERVAL);
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

    protected bool LoadGame()
    {
      byte[] gameData = _retroEmulator.SystemInfo.NeedsFullPath ? null : File.ReadAllBytes(_gamePath);
      return _retroEmulator.LoadGame(_gamePath, gameData);
    }

    protected void DoRender()
    {
      while (_doRender)
      {
        RunEmulator();
        RenderFrame();
        CheckPauseState();
      }
      OnRenderThreadFinished();
    }

    protected void RunEmulator()
    {
      if (_syncToAudio || NeedsRender())
      {
        _retroEmulator.Run();
        UpdateAudioVideo();
        if (_autoSave)
          _saveHandler.AutoSave();
      }
    }

    protected void UpdateAudioVideo()
    {
      if (_videoReady)
        UpdateVideo();
      if (_frameBufferReady)
        UpdateFrameBuffer();
      //Update audio last as we are potentially syncing our remaining frame time to audio
      if (_audioReady)
        UpdateAudio();
    }

    protected void UpdateVideo()
    {
      _videoReady = false;
      lock (_surfaceLock)
      {
        if (_guiInitialized)
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight);
      }
    }

    protected void UpdateFrameBuffer()
    {
      _frameBufferReady = false;
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
      _audioReady = false;
      lock (_audioLock)
      {
        if (_soundOutput != null)
          _soundOutput.WriteSamples(_retroEmulator.AudioBuffer.Data, _retroEmulator.AudioBuffer.Length, _syncToAudio);
      }
    }

    protected void OnVideoReady(object sender, EventArgs e)
    {
      _videoReady = true;
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      _frameBufferReady = true;
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
      _audioReady = true;
    }

    protected bool NeedsRender()
    {
      long currentTimestamp = Stopwatch.GetTimestamp();
      double secondsPassed = (double)(currentTimestamp - _lastRenderTimestamp) / Stopwatch.Frequency;
      if (secondsPassed < _secondsPerFrame)
        return false;
      _lastRenderTimestamp = currentTimestamp;
      return true;
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
      _retroEmulator.Dispose();
      _retroEmulator = null;
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

    protected void RenderFrame()
    {
      RenderDlgt dlgt = _renderDlgt;
      if (dlgt != null)
        dlgt();
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
    }
    #endregion
  }
}