using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators
{
    public class GamePlayer : IPlayer, IDisposable
    {
      protected PlayerState _state;
      protected string _mediaItemTitle;
      protected IResourceLocator _resourceLocator;
      protected IResourceAccessor _resourceAccessor;
      protected GameLauncher _gameLauncher;

      public string Name
      {
        get { return "Emulators Game Player"; }
      }

      public PlayerState State
      {
        get { return _state; }
      }

      public string MediaItemTitle
      {
        get { return _mediaItemTitle; }
      }

      public void SetMediaItem(IResourceLocator locator, string title, EmulatorConfiguration emulatorConfiguration)
      {
        _state = PlayerState.Active;
        _resourceLocator = locator;
        _mediaItemTitle = title;

        CreateResourceAccessor();

        _gameLauncher = new GameLauncher(emulatorConfiguration, _resourceAccessor.Path);
        _gameLauncher.GameExited += _gameLauncher_GameExited;
        _gameLauncher.Init();
        _gameLauncher.Launch();
      }

      void _gameLauncher_GameExited(object sender, EventArgs e)
      {
        Stop();
      }

      protected virtual void CreateResourceAccessor()
      {
        _resourceAccessor = _resourceLocator.CreateAccessor();
      }
            
      public void Stop()
      {
        _state = PlayerState.Ended;
      }

      public void Dispose()
      {
        if (_resourceAccessor != null)
        {
          _resourceAccessor.Dispose();
          _resourceAccessor = null;
        }
      }
    }
}
