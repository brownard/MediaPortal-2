using Emulators.Common.Emulators;
using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class GameMetadataExtractor : IMetadataExtractor
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public const string METADATAEXTRACTOR_ID_STR = "7ED0605F-E3B3-4B4A-AD58-AE56BC17A3E5";
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static MediaCategory _gameCategory;
    protected static ConcurrentDictionary<string, MediaCategory> _platformCategories = new ConcurrentDictionary<string, MediaCategory>();
    protected MetadataExtractorMetadata _metadata;
    protected readonly SettingsChangeWatcher<CommonSettings> _settingsWatcher = new SettingsChangeWatcher<CommonSettings>();

    static GameMetadataExtractor()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(GameCategory.CATEGORY_NAME, out _gameCategory))
        _gameCategory = mediaAccessor.RegisterMediaCategory(GameCategory.CATEGORY_NAME, null);
      MEDIA_CATEGORIES.Add(_gameCategory);
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(GameAspect.Metadata);
    }

    public static MediaCategory GameMediaCategory
    {
      get { return _gameCategory; }
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
      updateMediaCategories(_settingsWatcher.Settings.ConfiguredEmulators);
    }

    void settingsChanged(object sender, EventArgs e)
    {
      updateMediaCategories(_settingsWatcher.Settings.ConfiguredEmulators);
    }

    void updateMediaCategories(List<EmulatorConfiguration> configurations)
    {
      if (configurations == null || configurations.Count == 0)
        return;

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (EmulatorConfiguration configuration in configurations)
        foreach (string platform in configuration.Platforms)
          if (!mediaAccessor.MediaCategories.ContainsKey(platform))
          {
            Logger.Debug("GamesMetadataExtractor: Adding Game Category {0}", platform);
            MediaCategory category = mediaAccessor.RegisterMediaCategory(platform, new[] { _gameCategory });
            _platformCategories.TryAdd(platform, category);
          }
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
        Logger.Info("GamesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    static bool ExtractGameData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      var categories = ServiceRegistration.Get<IMediaCategoryHelper>().GetMediaCategories(lfsra.CanonicalLocalResourcePath);
      string platform = categories.FirstOrDefault(s => _platformCategories.ContainsKey(s));
      if (string.IsNullOrEmpty(platform))
      {
        Logger.Warn("GamesMetadataExtractor: Unable to import {0}, no platform categories have been selected", lfsra.LocalFileSystemPath);
        return false;
      }

      var configurations = ServiceRegistration.Get<ISettingsManager>().Load<CommonSettings>().ConfiguredEmulators;
      if (!HasGameExtension(lfsra.LocalFileSystemPath, platform, configurations))
        return false;

      Logger.Debug("GamesMetadataExtractor: Importing game: '{0}', '{1}'", lfsra.LocalFileSystemPath, platform);
      string name = DosPathHelper.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath);
      GameInfo gameInfo = new GameInfo()
      {
        GameName = name,
        Platform = platform
      };

      GameMatcher matcher = GameMatcher.Instance;
      if (matcher.FindAndUpdateGame(gameInfo))
      {
        gameInfo.SetMetadata(extractedAspectData);
      }
      else
      {
        Logger.Debug("GamesMetadataExtractor: No match found for game: '{0}', '{1}'", lfsra.LocalFileSystemPath, platform);
        gameInfo = new GameInfo()
        {
          GameName = name,
          Platform = platform
        };
        gameInfo.SetMetadata(extractedAspectData);
      }
      return true;
    }

    protected static bool HasGameExtension(string fileName, string platform, List<EmulatorConfiguration> configurations)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return configurations.Any(c => c.Platforms.Contains(platform) && (c.FileExtensions.Count == 0 || c.FileExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase)));
    }
  }
}