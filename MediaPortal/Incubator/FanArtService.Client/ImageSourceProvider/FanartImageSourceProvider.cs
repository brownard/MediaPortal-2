using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider
{
  public class FanartImageSourceProvider : IFanartImageSourceProvider
  {
    bool IFanartImageSourceProvider.TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource)
    {
      fanartImageSource = null;
      string fanArtMediaType = null;
      string fanArtName = null;

      SeriesFilterItem series = listItem as SeriesFilterItem;
      if (series != null)
      {
        fanArtMediaType = FanArtMediaTypes.Series;
        fanArtName = series.SimpleTitle;
      }
      SeriesItem episode = listItem as SeriesItem;
      if (episode != null)
      {
        fanArtMediaType = FanArtMediaTypes.Series;
        fanArtName = episode.Series;
      }
      MovieFilterItem movieCollection = listItem as MovieFilterItem;
      if (movieCollection != null)
      {
        fanArtMediaType = FanArtMediaTypes.MovieCollection;
        fanArtName = movieCollection.SimpleTitle;
      }
      MovieItem movie = listItem as MovieItem;
      if (movie != null)
      {
        fanArtMediaType = FanArtMediaTypes.Movie;
        // Fanart loading now depends on the MediaItemId to support local fanart
        fanArtName = movie.MediaItem.MediaItemId.ToString();
      }
      VideoItem video = listItem as VideoItem;
      if (video != null)
      {
        fanArtMediaType = FanArtMediaTypes.Movie;
        // Fanart loading now depends on the MediaItemId to support local fanart
        fanArtName = video.MediaItem.MediaItemId.ToString();
      }

      if (fanArtMediaType == null || fanArtName == null)
        return false;

      fanartImageSource = new FanArtImageSource()
      {
        FanArtMediaType = fanArtMediaType,
        FanArtName = fanArtName
      };
      return true;
    }
  }
}
