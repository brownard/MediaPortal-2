using Emulators.Common.TheGamesDb;
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
    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != FanArtConstants.FanArtMediaType.Undefined || string.IsNullOrWhiteSpace(name))
        return false;

      string path = Path.Combine(TheGamesDbWrapper.CACHE_PATH, name);
      switch (fanArtType)
      {
        case FanArtConstants.FanArtType.Poster:
          path = Path.Combine(path, @"Covers\front");
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

    protected string[] GetPatterns(FanArtConstants.FanArtType fanArtType)
    {
      return new string[] { "*.jpg", "*.png" };
    }
  }
}
