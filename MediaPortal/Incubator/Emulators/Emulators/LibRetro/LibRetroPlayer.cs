using MediaPortal.UI.SkinEngine.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Geometries;
using SharpDX.Direct3D9;
using MediaPortal.UI.Presentation.Players;
using System.Drawing;
using SharpRetro.LibRetro;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Emulators.LibRetro.Controllers;
using Emulators.LibRetro.VideoProviders;
using Emulators.LibRetro.SoundProviders;
using MediaPortal.Common.PathManager;
using SharpRetro.OpenGL;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement;
using Emulators.LibRetro.Renderers;

namespace Emulators.LibRetro
{
  public class LibRetroPlayer : ISharpDXVideoPlayer, IDisposable
  {
    const string CORE_PATH = @"E:\Games\Cores\catsfc_libretro.dll";
    const string GAME_PATH = @"E:\Games\SNES\Super Mario Kart (USA).sfc";

    const string CORE_PATH_PSX = @"E:\Games\Cores\beetle_psx_libretro.dll";
    const string GAME_PATH_PSX = @"E:\Games\PSX\Crash Team Racing [SCUS-94426].cue";

    const string CORE_PATH_SEGA = @"E:\Games\Cores\genesis_plus_gx_libretro.dll";
    const string GAME_PATH_SEGA = @"E:\Games\MegaDrive\Sonic the Hedgehog 2 (World).md";

    const string CORE_PATH_N64 = @"E:\Games\Cores\mupen64plus_libretro_st.dll";
    const string GAME_PATH_N64 = @"E:\Games\Cores\Banjo-Kazooie (E) (M3) [!].z64";
    //const string GAME_PATH_N64 = @"E:\Games\Cores\Mario Kart 64 (E) (V1.0) [!].z64";

    const string CORE_PATH_3D = @"C:\Users\Brownard\Downloads\RetroArch\3dengine_libretro_d.dll";
    const string GAME_PATH_3D = @"E:\Games\Cores\box.obj";

    public const string AUDIO_STREAM_NAME = "Audio1";
    protected static string[] DEFAULT_AUDIO_STREAM_NAMES = new[] { AUDIO_STREAM_NAME };
    protected static readonly string SAVE_DIRECTORY = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\LibRetro\");

    protected readonly object _syncObj = new object();
    protected bool _initialised;
    protected LibRetroEmulator _retroEmulator;
    protected double _vsync;
    protected volatile bool _doRender;
    protected Thread _renderThread;
    protected ITextureProvider _textureProvider;
    protected ISoundOutput _soundOutput;
    protected bool _syncToAudio = true;

    protected RenderDlgt _renderDlgt;
    protected PlayerState _state = PlayerState.Stopped;
    protected string _mediaItemTitle;
    protected bool _isMuted;
    protected int _volume;

    ILocalFsResourceAccessor _accessor;

    public void Play(MediaItem mediaItem)
    {
      if (_retroEmulator != null)
        return;

      //if (!mediaItem.GetResourceLocator().TryCreateLocalFsAccessor(out _accessor))
      //  return;

      _initialised = true;
      bool result;
      _retroEmulator = new LibRetroEmulator(CORE_PATH_N64)
      {
        SaveDirectory = SAVE_DIRECTORY,
        LogDelegate = RetroLogDlgt,
        Controller = new XInputController(false),
        GLContext = new OpenGLHelper()
      };
      _retroEmulator.VideoReady += OnVideoReady;
      _retroEmulator.FrameBufferReady += OnFrameBufferReady;
      _retroEmulator.AudioReady += OnAudioReady;
      _retroEmulator.Init();
      if (_retroEmulator.SystemInfo.NeedsFullPath)
        result = _retroEmulator.LoadGame(GAME_PATH_N64);
      else
        result = _retroEmulator.LoadGame(File.ReadAllBytes(GAME_PATH_N64), GAME_PATH_N64);
      if (!result)
        return;
      lock (_syncObj)
        _textureProvider = new LibRetroTextureWrapper();
      _soundOutput = new LibRetroDirectSound(SkinContext.Form.Handle, (int)_retroEmulator.TimingInfo.SampleRate);
      _vsync = 1 / _retroEmulator.TimingInfo.VSyncRate;
      _soundOutput.Play();
      _doRender = true;
      _renderThread = new Thread(DoRender);
      _renderThread.Start();
      _state = PlayerState.Active;
    }

    protected void OnVideoReady(object sender, EventArgs e)
    {
      lock (SurfaceLock)
        if (_initialised)
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight);
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      lock (SurfaceLock)
          if (_initialised)
          _textureProvider.UpdateTexture(SkinContext.Device, _retroEmulator.GLContext.Pixels, _retroEmulator.VideoInfo.BufferWidth, _retroEmulator.VideoInfo.BufferHeight, _retroEmulator.GLContext.BottomLeftOrigin);
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
      _soundOutput.WriteSamples(_retroEmulator.AudioBuffer.Data, _retroEmulator.AudioBuffer.Length, _syncToAudio);
    }

    protected void DoRender()
    {
      while (_doRender)
      {
        _retroEmulator.Run();
        RenderFrame();
      }
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

    protected void RenderFrame()
    {
      RenderDlgt dlgt = _renderDlgt;
      if (dlgt != null)
        dlgt();
    }

    protected void RetroLogDlgt(string message)
    {
      ServiceRegistration.Get<ILogger>().Info("LibRetro: {0}", message);
    }

    public string[] AudioStreams
    {
      get { return DEFAULT_AUDIO_STREAM_NAMES; }
    }

    public CropSettings CropSettings
    {
      get { return null; }
      set { }
    }

    public SharpDX.Rectangle CropVideoRect
    {
      get
      {
        SharpDX.Size2 videoSize = VideoSize.ToSize2();
        return new SharpDX.Rectangle(0, 0, videoSize.Width, videoSize.Height);
      }
    }

    public string CurrentAudioStream
    {
      get { return AudioStreams[0]; }
    }

    public string EffectOverride
    {
      get { return null; }
      set { }
    }

    public IGeometry GeometryOverride
    {
      get { return null; }
      set { }
    }

    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    public bool Mute
    {
      get { return _isMuted; }
      set { _isMuted = value; }
    }

    public string Name
    {
      get { return "LibRetroPlayer"; }
    }

    public PlayerState State
    {
      get { return _state; }
    }

    public object SurfaceLock
    {
      get { return _syncObj; }
    }

    public Texture Texture
    {
      get
      {
        lock (SurfaceLock)
          return _textureProvider != null ? _textureProvider.Texture : null;
      }
    }

    public SizeF VideoAspectRatio
    {
      get
      {
        if (_retroEmulator != null)
        {
          VideoInfo videoInfo = _retroEmulator.VideoInfo;
          if (videoInfo != null)
            return new SizeF(videoInfo.VirtualWidth, videoInfo.VirtualHeight);
        }
        return new SizeF(1, 1);
      }
    }

    public Size VideoSize
    {
      get
      {
        if (_retroEmulator != null)
        {
          VideoInfo videoInfo = _retroEmulator.VideoInfo;
          if (videoInfo != null)
            return new Size(videoInfo.BufferWidth, videoInfo.BufferHeight);
        }
        return new Size(0, 0);
      }
    }

    public int Volume
    {
      get { return _volume; }
      set { _volume = value; }
    }

    public void ReallocGUIResources()
    {
      lock (SurfaceLock)
        _initialised = true;
    }

    public void ReleaseGUIResources()
    {
      lock (SurfaceLock)
      {
        _initialised = false;
        if (_textureProvider != null)
          _textureProvider.Release();
      }
    }

    public void SetAudioStream(string audioStream)
    {

    }

    public bool SetRenderDelegate(RenderDlgt dlgt)
    {
      _renderDlgt = dlgt;
      return true;
    }

    public void Stop()
    {
      Dispose();
    }

    public void Dispose()
    {
      _doRender = false;
      if (_renderThread != null)
      {
        _renderThread.Join();
        _renderThread = null;
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
      if (_accessor != null)
      {
        _accessor.Dispose();
        _accessor = null;
      }
    }
  }
}