using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using Emulators.Common.Games;

namespace Emulators.LibRetro
{
  public class LibRetroPlayerBuilder : IPlayerBuilder
  {
    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      if (!mediaItem.Aspects.ContainsKey(GameAspect.ASPECT_ID))
        return null;
      var player = new LibRetroPlayer();
      player.Play();
      return player;
    }
  }
}
