using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.SkinManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Render
{
  public class SynchronisationStrategy
  {
    protected bool _allowVSync;
    protected volatile bool _doVSync;
    protected string _renderMode;
    protected double _targetFps;
    protected double _secondsPerFrame;
    protected long _lastRenderTimestamp;

    public SynchronisationStrategy(double targetFps, bool allowVSync)
    {
      _targetFps = targetFps;
      _secondsPerFrame = 1d / targetFps;
      _allowVSync = allowVSync;
      if (_allowVSync)
      {
        IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
        screenControl.VideoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate += SyncToPlayer;
      }
    }

    public void Synchronise(bool force)
    {
      double fps = SkinContext.RenderStrategy.TargetFrameRate;
      if (force || !_doVSync || System.Windows.Forms.Form.ActiveForm != SkinContext.Form)
        WaitForRenderTime();
    }

    public void Update()
    {
      if (!_doVSync)
        return;
      var context = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext;
      if (context != null && context.CurrentPlayer is LibRetroPlayer)
        SetVSyncStrategy();
    }

    public void Stop()
    {
      if (!_allowVSync)
        return;

      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
      screenControl.VideoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate -= SyncToPlayer;
      ResetVSyncStrategy();
    }

    protected void SyncToPlayer(IVideoPlayer player)
    {
      double fps = SkinContext.RenderStrategy.TargetFrameRate;
      if (player is LibRetroPlayer)
        SetVSyncStrategy();
      else
        ResetVSyncStrategy();
    }

    protected void SetVSyncStrategy()
    {
      if (SkinContext.RenderStrategy.Name.Contains("VSync"))
        return;
      _renderMode = SkinContext.RenderStrategy.Name;
      while (!SkinContext.RenderStrategy.Name.Contains("VSync"))
        SkinContext.NextRenderStrategy();
      _doVSync = true;
    }

    protected void ResetVSyncStrategy()
    {
      _doVSync = false;
      if (!string.IsNullOrEmpty(_renderMode))
        while (SkinContext.RenderStrategy.Name != _renderMode)
          SkinContext.NextRenderStrategy();
    }

    protected void WaitForRenderTime()
    {
      long currentTimestamp = Stopwatch.GetTimestamp();
      double secondsRemaining = GetRemainingRenderTime(currentTimestamp);
      while (secondsRemaining > 0)
      {
        Thread.Sleep(TimeSpan.FromSeconds(secondsRemaining / 2));
        currentTimestamp = Stopwatch.GetTimestamp();
        secondsRemaining = GetRemainingRenderTime(currentTimestamp);
      }
      _lastRenderTimestamp = currentTimestamp;
    }

    protected double GetRemainingRenderTime(long currentTimestamp)
    {
      double elapsed = (double)(currentTimestamp - _lastRenderTimestamp) / Stopwatch.Frequency;
      return _secondsPerFrame - elapsed;
    }
  }
}
