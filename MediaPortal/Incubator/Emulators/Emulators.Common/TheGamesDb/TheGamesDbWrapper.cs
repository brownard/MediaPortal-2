using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.NameProcessing;
using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Common.TheGamesDb
{
  public class TheGamesDbWrapper : BaseMatcher<GameMatch<int>, int>, IOnlineMatcher
  {
    #region Logger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region Consts
    protected const int API_VERSION = 1;
    protected static readonly Guid MATCHER_ID = new Guid("32047FBF-9080-4236-AE05-2E6DC1BF3A9F");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDB\");
    protected const string COVERS_DIRECTORY = "Covers";
    protected const string COVERS_FRONT = "front";
    protected const string COVERS_BACK = "back";
    protected const string FANART_DIRECTORY = "Fanart";
    protected const string BANNERS_DIRECTORY = "Banners";
    protected const string CLEARLOGO_DIRECTORY = "ClearLogos";
    protected const string SCREENSHOT_DIRECTORY = "Screenshots";
    protected const int MAX_SEARCH_DISTANCE = 2;
    protected const string BASE_URL = "http://thegamesdb.net/api/";
    protected const string SEARCH_PATH = "GetGamesList.php";
    protected const string GET_PATH = "GetGame.php?id=";
    protected static readonly CultureInfo DATE_CULTURE = CultureInfo.CreateSpecificCulture("en-US");
    protected static readonly Regex REGEX_ID = new Regex(@"[\[\(]gg(\d+)[\)\]]", RegexOptions.IgnoreCase);
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected const string PLATFORMS_XML = "Emulators.Common.TheGamesDb.PlatformsList.xml";
    #endregion

    #region Protected Members 
    protected static readonly object _platformsSync = new object();
    protected static Platform[] _platforms;
    protected XmlDownloader _downloader = new XmlDownloader() { Encoding = Encoding.UTF8 };
    protected MemoryCache<int, GameResult> _memoryCache = new MemoryCache<int, GameResult>();
    #endregion

    #region Static Methods
    protected static Platform[] LoadPlatforms()
    {
      XmlSerializer serializer = new XmlSerializer(typeof(PlatformsList));
      using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream(PLATFORMS_XML)))
        return ((PlatformsList)serializer.Deserialize(reader)).Platforms;
    }

    public static bool TryGetTGDBId(GameInfo gameInfo)
    {
      if (string.IsNullOrEmpty(gameInfo.GameName))
        return false;
      Match m = REGEX_ID.Match(gameInfo.GameName);
      if (m.Success)
      {
        gameInfo.GamesDbId = int.Parse(m.Groups[1].Value);
        return true;
      }
      return false;
    }
    #endregion

    #region Public Properties
    public static List<Platform> Platforms
    {
      get
      {
        lock (_platformsSync)
          if (_platforms == null)
            _platforms = LoadPlatforms();
        return new List<Platform>(_platforms);
      }
    }

    public Guid MatcherId
    {
      get { return MATCHER_ID; }
    }

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }
    #endregion

    #region Public Methods
    public override bool Init()
    {
      if (_storage == null)
      {
        //This is a workaround for the fact that more image types were added after the initial release of this plugin.
        //Force all images to be redownloaded if the they were completed before the changes.
        var settingsManager = ServiceRegistration.Get<ISettingsManager>();
        var settings = settingsManager.Load<TheGamesDbSettings>();
        if (settings.APIVersion < API_VERSION)
        {
          _storage = new MatchStorage<GameMatch<int>, int>(MatchesSettingsFile);
          var results = _storage.GetMatches();
          foreach (var result in results)
          {
            result.FanArtDownloadStarted = null;
            result.FanArtDownloadFinished = null;
          }
          _storage.SaveMatches();
          settings.APIVersion = API_VERSION;
          settingsManager.Save(settings);
        }
      }
      return base.Init();
    }

    public bool FindAndUpdateGame(GameInfo gameInfo)
    {
      GameResult result;
      if (!TryGetBestMatch(gameInfo, out result))
        return false;
      Game game = result.Game;
      gameInfo.GameName = game.GameTitle;
      gameInfo.GamesDbId = game.Id;
      gameInfo.MatcherId = MatcherId;
      gameInfo.OnlineId = game.Id.ToString();
      gameInfo.Certification = game.ESRB;
      gameInfo.Description = game.Overview;
      gameInfo.Developer = game.Developer;
      if (game.Genres != null && game.Genres.Genres != null)
        gameInfo.Genres.AddRange(game.Genres.Genres);
      gameInfo.Rating = game.Rating;
      DateTime releaseDate;
      if (DateTime.TryParse(game.ReleaseDate, DATE_CULTURE, DateTimeStyles.None, out releaseDate))
        gameInfo.ReleaseDate = releaseDate;
      if (game.Id > 0)
        ScheduleDownload(game.Id);
      return true;
    }

    public bool TryGetImagePath(string id, ImageType imageType, out string path)
    {
      path = null;
      if (string.IsNullOrEmpty(id))
        return false;

      switch (imageType)
      {
        case ImageType.FrontCover:
          path = Path.Combine(CACHE_PATH, id, COVERS_DIRECTORY, COVERS_FRONT);
          return true;
        case ImageType.BackCover:
          path = Path.Combine(CACHE_PATH, id, COVERS_DIRECTORY, COVERS_BACK);
          return true;
        case ImageType.Fanart:
          path = Path.Combine(CACHE_PATH, id, FANART_DIRECTORY);
          return true;
        case ImageType.Screenshot:
          path = Path.Combine(CACHE_PATH, id, SCREENSHOT_DIRECTORY);
          return true;
        case ImageType.Banner:
          path = Path.Combine(CACHE_PATH, id, BANNERS_DIRECTORY);
          return true;
        case ImageType.ClearLogo:
          path = Path.Combine(CACHE_PATH, id, CLEARLOGO_DIRECTORY);
          return true;
        default:
          return false;
      }
    }
    #endregion

    #region Protected Methods
    protected bool TryGetBestMatch(GameInfo gameInfo, out GameResult result)
    {
      result = null;
      try
      {
        if (gameInfo.GamesDbId > 0 && Get(gameInfo.GamesDbId, out result))
        {
          AddToStorage(result.Game.GameTitle, result.Game.Platform, result.Game.Id);
          return true;
        }

        GameMatch<int> match;
        if (TryGetFromStorage(gameInfo, out match))
        {
          return match.Id > 0 && Get(match.Id, out result);
        }
        else if (TryGetClosestMatch(gameInfo, out result))
        {
          AddToStorage(gameInfo.GameName, gameInfo.Platform, result.Game.Id);
          return true;
        }
        AddToStorage(gameInfo.GameName, gameInfo.Platform, 0);
        return false;
      }
      catch (Exception ex)
      {
        Logger.Debug("TheGamesDb: Exception processing game '{0}'", ex, gameInfo.GameName);
        return false;
      }
    }

    protected bool Search(string searchTerm, string platform, out List<GameSearchResult> results)
    {
      results = null;
      if (string.IsNullOrEmpty(searchTerm))
        return false;
      string query = "?name=" + HttpUtility.UrlEncode(searchTerm);
      if (!string.IsNullOrEmpty(platform))
        query += "&platform=" + HttpUtility.UrlEncode(platform);

      string url = string.Format("{0}{1}{2}", BASE_URL, SEARCH_PATH, query);
      GameSearchResults searchResults = _downloader.Download<GameSearchResults>(url);
      if (searchResults != null && searchResults.Results != null && searchResults.Results.Length > 0)
      {
        results = new List<GameSearchResult>(searchResults.Results);
        Logger.Debug("TheGamesDb: Found {0} search results for '{1}'", results.Count, searchTerm);
      }
      return results != null;
    }

    protected bool Get(int id, out GameResult result)
    {
      result = null;
      if (id < 1)
        return false;
      if (_memoryCache.TryGetValue(id, out result))
        return true;
      string cache = CreateAndGetCacheName(id);
      string url = string.Format("{0}{1}{2}", BASE_URL, GET_PATH, id);
      result = _downloader.Download<GameResult>(url, cache);
      if (result != null)
      {
        _memoryCache.Add(id, result);
        return true;
      }
      return false;
    }

    protected bool TryGetClosestMatch(GameInfo gameInfo, out GameResult result)
    {
      List<GameSearchResult> results;
      if (Search(gameInfo.GameName, gameInfo.Platform, out results))
      {
        results = results.FindAll(r => r.GameTitle == gameInfo.GameName || NameProcessor.GetLevenshteinDistance(r.GameTitle, gameInfo.GameName) <= MAX_SEARCH_DISTANCE);
        if (results.Count > 0 && Get(results[0].Id, out result))
          return true;
      }
      Logger.Debug("TheGamesDb: No match found for: '{0}'", gameInfo.GameName);
      result = null;
      return false;
    }

    protected override void DownloadFanArt(int itemId)
    {
      GameResult result;
      if (!Get(itemId, out result) || result.Game == null || result.Game.Images == null)
        return;

      GameImages images = result.Game.Images;
      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Begin saving images for IDd{0}", itemId);

      if (images.Boxart != null)
        foreach (GameImageBoxart image in images.Boxart)
          DownloadCover(itemId, image, result.BaseImgUrl);
      if (images.Fanart != null)
        foreach (GameImage fanart in images.Fanart)
          DownloadImage(itemId, fanart.Original, result.BaseImgUrl, FANART_DIRECTORY);
      if (images.Banner != null)
        foreach (GameImageOriginal banner in images.Banner)
          DownloadImage(itemId, banner, result.BaseImgUrl, BANNERS_DIRECTORY);
      if (images.ClearLogo != null)
        foreach (GameImageOriginal clearLogo in images.ClearLogo)
          DownloadImage(itemId, clearLogo, result.BaseImgUrl, CLEARLOGO_DIRECTORY);
      if (images.Screenshot != null)
        foreach (GameImage screenshot in images.Screenshot)
          DownloadImage(itemId, screenshot.Original, result.BaseImgUrl, SCREENSHOT_DIRECTORY);

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Finished saving images for IDd{0}", itemId);
      FinishDownloadFanArt(itemId);
    }

    protected void DownloadCover(int id, GameImageBoxart image, string baseUrl)
    {
      string url = baseUrl + image.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, Path.Combine(COVERS_DIRECTORY, image.Side), filename);
      _downloader.DownloadFile(url, downloadFile);
    }

    protected void DownloadImage(int id, GameImageOriginal image, string baseUrl, string category)
    {
      string url = baseUrl + image.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, category, filename);
      _downloader.DownloadFile(url, downloadFile);
    }
    #endregion

    #region Storage
    protected void AddToStorage(string searchTerm, string platform, int id)
    {
      var onlineMatch = new GameMatch<int>
      {
        Id = id,
        ItemName = string.Format("{0}:{1}", searchTerm, platform),
        GameName = searchTerm,
        Platform = platform
      };
      _storage.TryAddMatch(onlineMatch);
    }

    protected bool TryGetFromStorage(GameInfo gameInfo, out GameMatch<int> match)
    {
      List<GameMatch<int>> matches = _storage.GetMatches();
      match = matches.Find(m =>
          string.Equals(m.GameName, gameInfo.GameName, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, gameInfo.Platform, StringComparison.OrdinalIgnoreCase));
      return match != null;
    }

    protected string CreateAndGetCacheName(int id, string category, string filename)
    {
      try
      {
        string folder = Path.Combine(CACHE_PATH, id.ToString(), category);
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, filename);
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    protected string CreateAndGetCacheName(int gameId)
    {
      try
      {
        string folder = Path.Combine(CACHE_PATH, gameId.ToString());
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("game_{0}.xml", gameId));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }
    #endregion
  }
}