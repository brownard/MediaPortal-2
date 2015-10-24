using Emulators.Common.Games;
using Emulators.Common.TheGamesDb;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.FanartProvider
{
  public class GameFanartProvider : IFanArtProvider
  {
    private static readonly Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID, GameAspect.ASPECT_ID };

    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != FanArtConstants.FanArtMediaType.Undefined && fanArtType == FanArtConstants.FanArtType.Thumbnail)
        return false;

      int gameDbId;
      if (!TryGetGameDbId(name, out gameDbId))
        return false;

      string path = Path.Combine(TheGamesDbWrapper.CACHE_PATH, gameDbId.ToString());
      switch (fanArtType)
      {
        case FanArtConstants.FanArtType.Thumbnail:
        case FanArtConstants.FanArtType.Poster:
          path = Path.Combine(path, @"Covers\front");
          break;
        case FanArtConstants.FanArtType.FanArt:
          path = Path.Combine(path, @"Fanart");
          break;
        default:
          return false;
      }

      List<IResourceLocator> files = new List<IResourceLocator>();
      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        if (directoryInfo.Exists)
        {
          foreach (string pattern in GetPatterns(fanArtType))
          {
            files.AddRange(directoryInfo.GetFiles(pattern)
              .Select(f => f.FullName)
              .Select(fileName => new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, fileName)))
              );
            result = files;
            if (result.Count > 0)
              return true;
          }
        }
      }
      catch (Exception) { }
      return false;
    }

    //public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    //{
    //  result = null;
    //  if (mediaType != FanartTypes.MEDIA_TYPE_GAME && mediaType != FanArtMediaTypes.Undefined && fanArtType == FanArtTypes.Thumbnail)
    //    return false;
      
    //  int gameDbId;
    //  if (!TryGetGameDbId(name, out gameDbId))
    //    return false;

    //  string path = Path.Combine(TheGamesDbWrapper.CACHE_PATH, gameDbId.ToString());
    //  switch (fanArtType)
    //  {
    //    case FanArtTypes.Thumbnail:
    //    case FanArtTypes.Poster:
    //      path = Path.Combine(path, @"Covers\front");
    //      break;
    //    case FanArtTypes.FanArt:
    //      path = Path.Combine(path, @"Fanart");
    //      break;
    //    default:
    //      return false;
    //  }

    //  List<IResourceLocator> files = new List<IResourceLocator>();
    //  try
    //  {
    //    DirectoryInfo directoryInfo = new DirectoryInfo(path);
    //    if (directoryInfo.Exists)
    //    {
    //      foreach (string pattern in GetPatterns(fanArtType))
    //      {
    //        files.AddRange(directoryInfo.GetFiles(pattern)
    //          .Select(f => f.FullName)
    //          .Select(fileName => new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, fileName)))
    //          );
    //        result = files;
    //        if (result.Count > 0)
    //          return true;
    //      }
    //    }
    //  }
    //  catch (Exception) { }
    //  return false;
    //}

    protected bool TryGetGameDbId(string name, out int gameDbId)
    {
      gameDbId = 0;
      Guid mediaItemId;
      if (Guid.TryParse(name, out mediaItemId) && mediaItemId != Guid.Empty)
      {
        IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
        if (mediaLibrary == null)
          return false;

        IFilter filter = new MediaItemIdFilter(mediaItemId);
        IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, filter), false);
        if (items == null || items.Count == 0)
          return false;

        MediaItem mediaItem = items.First();
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GameAspect.ATTR_TGDB_ID, out gameDbId))
          return true;
      }
      else if (int.TryParse(name, out gameDbId))
      {
        return true;
      }
      return false;
    }

    protected string[] GetPatterns(FanArtConstants.FanArtType fanArtType)
    {
      return new string[] { "*.jpg", "*.png" };
    }
  }
}