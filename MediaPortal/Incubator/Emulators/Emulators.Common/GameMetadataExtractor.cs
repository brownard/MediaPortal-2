using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class GameMetadataExtractor : IMetadataExtractor
  {
    public const string METADATAEXTRACTOR_ID_STR = "7ED0605F-E3B3-4B4A-AD58-AE56BC17A3E5";
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);
    protected const string MEDIA_CATEGORY_NAME_GAME = "Game";

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    static GameMetadataExtractor()
    {
      MediaCategory gameCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_GAME, out gameCategory))
        gameCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_GAME, null);
      MEDIA_CATEGORIES.Add(gameCategory);
    }

    public GameMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Games metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                GameAspect.Metadata
              });
    }

    static bool ExtractGameData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, "Game 1");
      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_MIME_TYPE, "game/nintendo-64");
      MediaItemAspect.SetAttribute(extractedAspectData, GameAspect.ATTR_PLATFORM_ID, 1);
      return true;
    }

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null || !fsra.IsFile)
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return ExtractGameData(rah.LocalFsResourceAccessor, extractedAspectData);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("GamesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }
  }
}
