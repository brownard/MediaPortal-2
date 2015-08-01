using Emulators.Common;
using Emulators.Common.FanartProvider;
using Emulators.Models.Navigation;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.UserServices.FanArtService.Client;
using MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Fanart
{
  public class FanartImageSourceProvider : IFanartImageSourceProvider
  {
    public bool TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource)
    {
      fanartImageSource = null;
      GameItem game = listItem as GameItem;
      MediaItemAspect aspect;
      if (game == null || !game.MediaItem.Aspects.TryGetValue(GameAspect.ASPECT_ID, out aspect))
        return false;

      int? id = (int?)aspect[GameAspect.ATTR_TGDB_ID];
      if (!id.HasValue)
        return false;

      fanartImageSource = new FanArtImageSource()
      {
        FanArtMediaType = FanartTypes.MEDIA_TYPE_GAME,
        FanArtName = id.Value.ToString()
      };
      return true;
    }
  }
}
