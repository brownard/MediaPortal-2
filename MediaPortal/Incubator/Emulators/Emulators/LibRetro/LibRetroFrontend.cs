using Emulators.LibRetro.Controllers;
using Emulators.LibRetro.Renderers;
using Emulators.LibRetro.SoundProviders;
using Emulators.LibRetro.VideoProviders;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
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
    protected readonly object _syncObj = new object();
    protected LibRetroEmulator _retroEmulator;
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
    protected RenderDlgt _renderDlgt;
    protected bool _isPaused;
    protected ManualResetEventSlim _pauseWaitHandle;
    #endregion

    #region Ctor
    public LibRetroFrontend(string corePath, string gamePath, string saveDirectory)
    {
      _corePath = corePath;
      _gamePath = gamePath;
      _saveDirectory = saveDirectory;
      _pauseWaitHandle = new ManualResetEventSlim(true);
    }
    #endregion

    #region Public Properties
    public object SyncObj
    {
      get { return _syncObj; }
    }

    public bool Paused
    {
      get { return _isPaused; }
    }

    public Texture Texture
    {
      get
      {
        lock (_syncObj)
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
      _retroEmulator = new LibRetroEmulator(_corePath)
      {
        SaveDirectory = _saveDirectory,
        LogDelegate = RetroLogDlgt,
        Controller = new XInputController(false),
        GLContext = new OpenGLHelper()
      };
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
      LoadState();
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
      lock (_syncObj)
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
      lock (_syncObj)
        _initialised = true;
    }

    public void ReleaseGUIResources()
    {
      lock (_syncObj)
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
    protected void DoRender()
    {
      long timestamp = Stopwatch.GetTimestamp();
      while (_doRender)
      {
        lock (_syncObj)
          if (_pauseWaitHandle.IsSet && (_syncToAudio || NeedsRender(ref timestamp)))
            _retroEmulator.Run();
        RenderFrame();
        if (!_pauseWaitHandle.IsSet)
          _pauseWaitHandle.Wait();
      }
      SaveState();
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
      lock (_syncObj)
      {
        if (_isPaused)
        {
          if (_soundOutput != null)
            _soundOutput.Pause();
          if (_pauseWaitHandle != null)
            _pauseWaitHandle.Reset();
        }
        else
        {
          if (_soundOutput != null)
            _soundOutput.UnPause();
          if (_pauseWaitHandle != null)
            _pauseWaitHandle.Set();
        }
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
      if (_initialised)
        _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight);
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      if (_initialised)
        _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.GLContext.Pixels, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight, _retroEmulator.GLContext.BottomLeftOrigin);
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
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

    protected void LoadState()
    {
      string saveFile = GetSaveFile();
      byte[] saveState;
      if (TryReadFromFile(GetSaveFile(), out saveState))
        _retroEmulator.LoadState(LibRetroCore.RETRO_MEMORY.SAVE_RAM, saveState);
    }

    protected void SaveState()
    {
      byte[] saveState = _retroEmulator.SaveState(LibRetroCore.RETRO_MEMORY.SAVE_RAM);
      if (saveState == null)
        return;
      TryWriteToFile(GetSaveFile(), saveState);
    }

    protected bool TryReadFromFile(string path, out byte[] fileBytes)
    {
      try
      {
        if (File.Exists(path))
        {
          fileBytes = File.ReadAllBytes(path);
          return true;
        }
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroFrontend: Error reading from path '{0}':", ex, path);
      }
      fileBytes = null;
      return false;
    }

    protected bool TryWriteToFile(string path, byte[] fileBytes)
    {
      try
      {
        DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(path));
        if (!directory.Exists)
          directory.Create();
        File.WriteAllBytes(path, fileBytes);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroFrontend: Error writing to path '{0}':", ex, path);
      }
      return false;
    }

    protected string GetSaveFile()
    {
      return Path.Combine(_saveDirectory, Path.GetFileNameWithoutExtension(_gamePath) + ".srm");
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