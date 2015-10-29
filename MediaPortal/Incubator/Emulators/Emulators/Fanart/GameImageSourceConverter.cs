using Emulators.Common;
using Emulators.Common.FanartProvider;
using Emulators.Common.Games;
using Emulators.Models.Navigation;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.UserServices.FanArtService.Client;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Fanart
{
  public class GameImageSourceConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      result = null;
      MediaItem mediaItem = val as MediaItem;
      if (mediaItem == null || mediaItem.MediaItemId == Guid.Empty || !mediaItem.Aspects.ContainsKey(GameAspect.ASPECT_ID))
        return false;

      string param = parameter as string;
      if (string.IsNullOrEmpty(param))
        return false;

      var args = param.Split(';');
      if (args.Length < 3)
        return false;

      FanArtConstants.FanArtType fanartType;
      if (!Enum.TryParse(args[0], out fanartType))
        return false;

      int maxWidth;
      int maxHeight;
      int.TryParse(args[1], out maxWidth);
      int.TryParse(args[2], out maxHeight);

      bool useCache = true;
      if (args.Length == 4 && !bool.TryParse(args[3], out useCache))
        useCache = true;

      result = new FanArtImageSource
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.Undefined, //FanartTypes.MEDIA_TYPE_GAME,
        FanArtName = mediaItem.MediaItemId.ToString(),
        FanArtType = FanArtConstants.FanArtType.Thumbnail, //fanartType,
        MaxWidth = maxWidth,
        MaxHeight = maxHeight,
        Cache = useCache
      };

      return true;
    }
  }
}
