using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators
{
  public class GamePlayerBuilder : IPlayerBuilder
  {
    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      string mimeType, title;
      if (!mediaItem.GetPlayData(out mimeType, out title))
        return null;

      if (mimeType.StartsWith("game/"))
      {
        GamePlayer player = new GamePlayer();
        player.SetMediaItem(mediaItem.GetResourceLocator(), title, new PJ64Config());
        return player;
      }
      return null;
    }
  }
}
