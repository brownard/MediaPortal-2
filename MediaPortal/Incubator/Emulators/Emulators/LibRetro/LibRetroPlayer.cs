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
using MediaPortal.UI.SkinEngine;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement;

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
    const string GAME_PATH_N64 = @"E:\Games\Banjo-Kazooie (E) (M3) [!].z64";
    //const string GAME_PATH_N64 = @"E:\Games\Cores\Mario Kart 64 (E) (V1.0) [!].z64";

    const string CORE_PATH_3D = @"C:\Users\Brownard\Downloads\RetroArch\3dengine_libretro_d.dll";
    const string GAME_PATH_3D = @"E:\Games\Cores\box.obj";

    #region Protected Members
    protected const string AUDIO_STREAM_NAME = "Audio1";
    protected static string[] DEFAULT_AUDIO_STREAM_NAMES = new[] { AUDIO_STREAM_NAME };
    protected static readonly string SAVE_DIRECTORY = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\LibRetro\");

    protected readonly object _syncObj = new object();
    protected LibRetroFrontend _retro;
    protected PlayerState _state = PlayerState.Stopped;
    protected string _mediaItemTitle;
    protected bool _isMuted;
    protected int _volume;
    protected ILocalFsResourceAccessor _accessor;
    #endregion

    #region IPlayer
    public string MediaItemTitle
    {
      get { return _mediaItemTitle; }
    }

    public string Name
    {
      get { return "LibRetroPlayer"; }
    }

    public PlayerState State
    {
      get { return _state; }
    }

    public void Play(MediaItem mediaItem)
    {
      if (_retro != null)
        return;

      //if (!mediaItem.GetResourceLocator().TryCreateLocalFsAccessor(out _accessor))
      //  return;

      string corePath = CORE_PATH_N64;
      string gamePath = GAME_PATH_N64;
      _retro = new LibRetroFrontend(corePath, gamePath, SAVE_DIRECTORY);
      if (_retro.Init())
      {
        _retro.Run();
        _state = PlayerState.Active;
      }
    }

    public void Stop()
    {
      Dispose();
    }
    #endregion

    #region Video
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

    public object SurfaceLock
    {
      get { return _retro != null ? _retro.SyncObj : _syncObj; }
    }

    public Texture Texture
    {
      get { return _retro != null ? _retro.Texture : null; }
    }

    public SizeF VideoAspectRatio
    {
      get
      {
        if (_retro != null)
        {
          VideoInfo videoInfo = _retro.GetVideoInfo();
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
        if (_retro != null)
        {
          VideoInfo videoInfo = _retro.GetVideoInfo();
          if (videoInfo != null)
            return new Size(videoInfo.BufferWidth, videoInfo.BufferHeight);
        }
        return new Size(0, 0);
      }
    }

    public void ReallocGUIResources()
    {
      if (_retro != null)
        _retro.ReallocGUIResources();
    }

    public void ReleaseGUIResources()
    {
      if (_retro != null)
        _retro.ReleaseGUIResources();
    }

    public bool SetRenderDelegate(RenderDlgt dlgt)
    {
      return _retro != null && _retro.SetRenderDelegate(dlgt);
    }
    #endregion

    #region Audio
    public string[] AudioStreams
    {
      get { return DEFAULT_AUDIO_STREAM_NAMES; }
    }

    public string CurrentAudioStream
    {
      get { return AudioStreams[0]; }
    }

    public bool Mute
    {
      get { return _isMuted; }
      set
      {
        if (value == _isMuted)
          return;
        _isMuted = value;
        CheckAudio();
      }
    }

    public int Volume
    {
      get { return _volume; }
      set
      {
        _volume = value;
        CheckAudio();
      }
    }

    protected void CheckAudio()
    {
      int volume = _isMuted ? 0 : _volume;
      if (_retro != null)
        _retro.SetVolume(VolumeToHundredthDeciBel(volume));
    }

    /// <summary>
    /// Helper method for calculating the hundredth decibel value, needed by DirectSound
    /// (in the range from -10000 to 0), which is logarithmic, from our volume (in the range from 0 to 100),
    /// which is linear.
    /// </summary>
    /// <param name="volume">Volume in the range from 0 to 100, in a linear scale.</param>
    /// <returns>Volume in the range from -10000 to 0, in a logarithmic scale.</returns>
    protected static int VolumeToHundredthDeciBel(int volume)
    {
      return (int)((Math.Log10(volume * 99f / 100f + 1) - 2) * 5000);
    }

    public void SetAudioStream(string audioStream) { }
    #endregion

    #region IDisposable
    public void Dispose()
    {
      if (_retro != null)
      {
        _retro.Dispose();
        _retro = null;
      }
      if (_accessor != null)
      {
        _accessor.Dispose();
        _accessor = null;
      }
    }
    #endregion
  }
}