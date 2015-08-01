using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
  class TheGamesDbWrapper
  {
    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDB\");
    const string PLATFORMS_XML = "Emulators.Common.TheGamesDb.PlatformsList.xml";
    const string BASE_URL = "http://thegamesdb.net/api/";
    const string SEARCH_PATH = "GetGamesList.php";
    const string GET_PATH = "GetGame.php?id=";

    ConcurrentDictionary<int, GameResult> resultsCache = new ConcurrentDictionary<int, GameResult>();
    Downloader downloader = new Downloader();

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

    #endregion

    public GameSearchResult[] Search(string searchTerm, string platform)
    {
      if (string.IsNullOrEmpty(searchTerm))
        return null;
      string query = "?name=" + HttpUtility.UrlEncode(searchTerm);
      if (!string.IsNullOrEmpty(platform))
        query += "&platform=" + HttpUtility.UrlEncode(platform);

      string url = string.Format("{0}{1}{2}", BASE_URL, SEARCH_PATH, query);
      var results = downloader.Download<GameSearchResults>(url);
      return results.Results;
    }

    public GameResult Get(int id)
    {
      GameResult result;
      if (resultsCache.TryGetValue(id, out result))
        return result;
      string url = string.Format("{0}{1}{2}", BASE_URL, GET_PATH, id);
      result = downloader.Download<GameResult>(url);
      if (result != null)
        resultsCache.TryAdd(id, result);
      return result;
    }

    public void DownloadCovers(int id)
    {
      GameResult result = Get(id);
      if (result == null)
        return;

      foreach (GameImageBoxart image in result.Game.Images.Boxart)
        DownloadCover(id, image, result.BaseImgUrl);   
    }

    protected void DownloadCover(int id, GameImageBoxart image, string baseUrl)
    {
      string url = baseUrl + image.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, "Covers\\" + image.Side, filename);
      downloader.DownloadFile(url, downloadFile);
    }

    public void DownloadFanart(int id)
    {
      GameResult result = Get(id);
      if (result == null)
        return;

      foreach (GameImage image in result.Game.Images.Fanart)
        DownloadFanart(id, image, result.BaseImgUrl); 
    }

    protected void DownloadFanart(int id, GameImage image, string baseUrl)
    {
      string url = baseUrl + image.Original.Value;
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, "Fanart", filename);
      downloader.DownloadFile(url, downloadFile);
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
  }
}
