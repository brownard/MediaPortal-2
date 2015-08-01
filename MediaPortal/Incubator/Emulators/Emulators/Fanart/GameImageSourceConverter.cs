using Emulators.Common;
using Emulators.Common.FanartProvider;
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
      MediaItemAspect aspect;
      if (mediaItem == null || !mediaItem.Aspects.TryGetValue(GameAspect.ASPECT_ID, out aspect))
        return false;

      int? id = (int?)aspect[GameAspect.ATTR_TGDB_ID];
      if (!id.HasValue)
        return false;

      string param = parameter as string;
      if (string.IsNullOrEmpty(param))
        return false;

      var args = param.Split(';');
      if (args.Length < 3)
        return false;

      string fanartType = args[0];

      int maxWidth;
      int maxHeight;
      int.TryParse(args[1], out maxWidth);
      int.TryParse(args[2], out maxHeight);

      bool useCache = true;
      if (args.Length == 4 && !bool.TryParse(args[3], out useCache))
        useCache = true;

      result = new FanArtImageSource
      {
        FanArtMediaType = FanartTypes.MEDIA_TYPE_GAME,
        FanArtName = id.Value.ToString(),
        FanArtType = fanartType,
        MaxWidth = maxWidth,
        MaxHeight = maxHeight,
        Cache = useCache
      };

      return true;
    }
  }
}
