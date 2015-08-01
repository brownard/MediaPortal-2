using Emulators.Common.Settings;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
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
    protected static MediaCategory _gameCategory;
    protected MetadataExtractorMetadata _metadata;
    protected readonly SettingsChangeWatcher<PluginSettings> _settingsWatcher = new SettingsChangeWatcher<PluginSettings>();

    static GameMetadataExtractor()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_GAME, out _gameCategory))
        _gameCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_GAME, null);
      MEDIA_CATEGORIES.Add(_gameCategory);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(GameAspect.Metadata);
    }

    public GameMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Games metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                GameAspect.Metadata
              });

      _settingsWatcher.SettingsChanged += settingsChanged;
      updateMediaCategories(_settingsWatcher.Settings.ConfiguredPlatforms);
    }

    void settingsChanged(object sender, EventArgs e)
    {
      updateMediaCategories(_settingsWatcher.Settings.ConfiguredPlatforms);
    }

    void updateMediaCategories(List<string> mediaCategories)
    {
      if (mediaCategories == null || mediaCategories.Count == 0)
        return;

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (string category in mediaCategories)
        if (!mediaAccessor.MediaCategories.ContainsKey(category))
          mediaAccessor.RegisterMediaCategory(category, new[] { _gameCategory });
    }

    static bool ExtractGameData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      var categories = ServiceRegistration.Get<IMediaCategoryHelper>().GetMediaCategories(lfsra.CanonicalLocalResourcePath);
      string platform = categories.FirstOrDefault(s => s != _gameCategory.CategoryName);

      GameInfo gameInfo = new GameInfo()
      {
        GameName = DosPathHelper.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath),
        Platform = platform
      };

      GameMatcher matcher = new GameMatcher();
      if (matcher.FindAndUpdateGame(gameInfo))
      {
        gameInfo.SetMetadata(extractedAspectData);
        matcher.DownloadCovers(gameInfo.GamesDbId);
        matcher.DownloadFanart(gameInfo.GamesDbId);
        return true;
      }
      return false;
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
