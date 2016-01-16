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

namespace Emulators.LibRetro
{
  public class LibRetroPlayer : ISharpDXVideoPlayer, IMediaPlaybackControl, IDisposable
  {
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
    protected CropSettings _cropSettings;
    protected IGeometry _geometryOverride;
    protected ILocalFsResourceAccessor _accessor;
    #endregion

    #region Ctor
    public LibRetroPlayer()
    {
      _cropSettings = ServiceRegistration.Get<IGeometryManager>().CropSettings;
    }
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

    public void Play(LibRetroMediaItem mediaItem)
    {
      if (_retro != null)
        return;

      string gamePath;
      if (!string.IsNullOrEmpty(mediaItem.ExtractedPath))
        gamePath = mediaItem.ExtractedPath;
      else if (mediaItem.GetResourceLocator().TryCreateLocalFsAccessor(out _accessor))
        gamePath = _accessor.LocalFileSystemPath;
      else
        return;
      
      _retro = new LibRetroFrontend(mediaItem.LibRetroPath, gamePath, SAVE_DIRECTORY);
      if (_retro.Init())
      {
        _retro.Run();
        _state = PlayerState.Active;
      }
    }

    public void Stop()
    {
      Dispose();
      _state = PlayerState.Stopped;
    }
    #endregion

    #region IMediaPlaybackControl
    public bool SetPlaybackRate(double value)
    {
      return false;
    }

    public void Pause()
    {
      if (_retro != null)
        _retro.Pause();
    }

    public void Resume()
    {
      if (_retro != null)
        _retro.Unpause();
    }

    public void Restart()
    {

    }

    public TimeSpan CurrentTime
    {
      get { return TimeSpan.Zero; }
      set { }
    }

    public TimeSpan Duration
    {
      get { return TimeSpan.Zero; }
    }

    public double PlaybackRate
    {
      get { return 1; }
    }

    public bool IsPlayingAtNormalRate
    {
      get { return true; }
    }

    public bool IsSeeking
    {
      get { return false; }
    }

    public bool IsPaused
    {
      get { return _retro != null ? _retro.Paused : false; }
    }

    public bool CanSeekForwards
    {
      get { return false; }
    }

    public bool CanSeekBackwards
    {
      get { return false; }
    }
    #endregion

    #region Video
    public CropSettings CropSettings
    {
      get { return _cropSettings; }
      set { _cropSettings = value; }
    }

    public SharpDX.Rectangle CropVideoRect
    {
      get
      {
        SharpDX.Size2 videoSize = VideoSize.ToSize2();
        return _cropSettings == null ? new SharpDX.Rectangle(0, 0, videoSize.Width, videoSize.Height) : _cropSettings.CropRect(videoSize.ToDrawingSize()).ToRect();
      }
    }

    public string EffectOverride
    {
      get { return null; }
      set { }
    }

    public IGeometry GeometryOverride
    {
      get { return _geometryOverride; }
      set { _geometryOverride = value; }
    }

    public object SurfaceLock
    {
      get { return _retro != null ? _retro.SurfaceLock : _syncObj; }
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