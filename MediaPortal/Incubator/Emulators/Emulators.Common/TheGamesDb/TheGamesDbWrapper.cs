using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.NameProcessing;
using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Common.TheGamesDb
{
  public class TheGamesDbWrapper : BaseMatcher<GameMatch, int>, IOnlineMatcher
  {
    #region Logger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    public static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDB\");
    protected const int MAX_SEARCH_DISTANCE = 2;
    protected const string PLATFORMS_XML = "Emulators.Common.TheGamesDb.PlatformsList.xml";
    protected const string BASE_URL = "http://thegamesdb.net/api/";
    protected const string SEARCH_PATH = "GetGamesList.php";
    protected const string GET_PATH = "GetGame.php?id=";
    protected static readonly CultureInfo DATE_CULTURE = CultureInfo.CreateSpecificCulture("en-US");
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");

    protected Downloader _downloader = new Downloader();

    #region Static Members

    static TheGamesDbWrapper()
    {
      LoadPlatforms();
    }

    static void LoadPlatforms()
    {
      XmlSerializer serializer = new XmlSerializer(typeof(PlatformsList));
      using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream(PLATFORMS_XML)))
        Platforms = ((PlatformsList)serializer.Deserialize(reader)).Platforms;
    }

    public static Platform[] Platforms { get; internal set; }

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    public bool TryGetBestMatch(GameInfo gameInfo)
    {
      GameResult result;
      if (TryGetFromStorage(gameInfo.GameName, gameInfo.Platform, out result))
      {
        Logger.Debug("TheGamesDb: Retrieved from cache: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        UpdateGameInfo(gameInfo, result);
        return true;
      }

      List<GameSearchResult> results;
      if (!Search(gameInfo.GameName, gameInfo.Platform, out results))
      {
        Logger.Debug("TheGamesDb: No results found for '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return false;
      }

      Logger.Debug("TheGamesDb: Found {0} search results for '{1}' - '{2}'", results.Count, gameInfo.GameName, gameInfo.Platform);
      results = results.FindAll(r => r.GameTitle == gameInfo.GameName || NameProcessor.GetLevenshteinDistance(r.GameTitle, gameInfo.GameName) <= MAX_SEARCH_DISTANCE);
      if (results.Count == 0 || !Get(results[0].Id, out result))
      {
        Logger.Debug("TheGamesDb: No close match found for: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return false;
      }

      Logger.Debug("TheGamesDb: Matched '{0}' to '{1}' - '{2}'", results[0].GameTitle, gameInfo.GameName, gameInfo.Platform);
      AddToStorage(gameInfo.GameName, gameInfo.Platform, result.Game.Id);
      UpdateGameInfo(gameInfo, result);
      return true;
    }

    public bool Search(string searchTerm, string platform, out List<GameSearchResult> results)
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
        results = new List<GameSearchResult>(searchResults.Results);
      return results != null;
    }

    public bool Get(int id, out GameResult result)
    {
      string cache = CreateAndGetCacheName(id);
      string url = string.Format("{0}{1}{2}", BASE_URL, GET_PATH, id);
      result = _downloader.Download<GameResult>(url, cache);
      return result != null;
    }

    public void AddToStorage(string searchTerm, string platform, int id)
    {
      var onlineMatch = new GameMatch
      {
        Id = id,
        ItemName = searchTerm,
        Platform = platform
      };
      _storage.TryAddMatch(onlineMatch);
    }

    public bool TryGetFromStorage(string searchTerm, string platform, out GameResult result)
    {
      List<GameMatch> matches = _storage.GetMatches();
      GameMatch match = matches.Find(m =>
          string.Equals(m.ItemName, searchTerm, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, platform, StringComparison.OrdinalIgnoreCase));

      if (match != null && match.Id > 0)
        return Get(match.Id, out result);
      result = null;
      return false;
    }

    protected void UpdateGameInfo(GameInfo gameInfo, GameResult gameResult)
    {
      Game game = gameResult.Game;
      gameInfo.GameName = game.GameTitle;
      gameInfo.GamesDbId = game.Id;
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
    }

    protected override void DownloadFanArt(int itemId)
    {
      GameResult result;
      if (!Get(itemId, out result) || result.Game == null || result.Game.Images == null)
        return;

      GameImages images = result.Game.Images;
      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Begin saving images for IDd{0}", itemId);
      if (images.Boxart != null)
        foreach (GameImageBoxart image in result.Game.Images.Boxart)
          DownloadCover(itemId, image, result.BaseImgUrl);
      if (images.Fanart != null)
        foreach (GameImage fanart in result.Game.Images.Fanart)
          DownloadGameFanart(itemId, fanart, result.BaseImgUrl);
      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Finished saving images for IDd{0}", itemId);
      FinishDownloadFanArt(itemId);
    }

    protected void DownloadCover(int id, GameImageBoxart image, string baseUrl)
    {
      string url = baseUrl + image.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, "Covers\\" + image.Side, filename);
      _downloader.DownloadFile(url, downloadFile);
    }

    protected void DownloadGameFanart(int id, GameImage image, string baseUrl)
    {
      string url = baseUrl + image.Original.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, "Fanart", filename);
      _downloader.DownloadFile(url, downloadFile);
    }

    protected string CreateAndGetCacheName(int id, string category, string filename)
    {
      try
      {
        string folder = Path.Combine(CACHE_PATH, string.Format(@"{0}\{1}", id, category));
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
  }
}
