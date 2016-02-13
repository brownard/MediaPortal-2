using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  public class CoreInfoHandler
  {
    protected const string BASE_URL = "http://buildbot.libretro.com";
    protected const string INFO_URL = "/assets/frontend/info/";
    
    protected HtmlDownloader _downloader;
    protected Dictionary<string, CoreInfo> _coreInfos;

    public CoreInfoHandler()
    {
      _downloader = new HtmlDownloader();
      _coreInfos = new Dictionary<string, CoreInfo>();
    }

    public Dictionary<string, CoreInfo> CoreInfos
    {
      get { return _coreInfos; }
    }

    public void Update(string infoDirectory)
    {
      DownloadCoreInfos(infoDirectory);
      LoadCoreInfos(infoDirectory);
    }

    public void Load(string infoDirectory)
    {
      LoadCoreInfos(infoDirectory);
    }

    protected void DownloadCoreInfos(string infoDirectory)
    {
      if (!TryCreateInfoDirectory(infoDirectory))
        return;

      CoreInfoList infoList = _downloader.Download<CoreInfoList>(BASE_URL + INFO_URL);
      if (infoList == null)
        return;

      foreach (string infoUrl in infoList.CoreInfoUrls)
      {
        Uri uri;
        if (!Uri.TryCreate(infoUrl, UriKind.RelativeOrAbsolute, out uri))
          continue;
        if (!uri.IsAbsoluteUri)
          uri = new Uri(new Uri(BASE_URL), uri);
        string path = Path.Combine(infoDirectory, Path.GetFileName(uri.LocalPath));
        _downloader.DownloadFile(uri.AbsoluteUri, path);
      }
    }

    protected void LoadCoreInfos(string infoDirectory)
    {
      _coreInfos.Clear();
      if (!Directory.Exists(infoDirectory))
        return;

      try
      {
        foreach (string file in Directory.EnumerateFiles(infoDirectory, "*.info"))
        {
          CoreInfo coreInfo;
          if (TryLoadCoreInfo(file, out coreInfo))
            _coreInfos[coreInfo.CoreName] = coreInfo;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception reading directory '{0}'", ex, infoDirectory);
      }
    }

    protected bool TryLoadCoreInfo(string path, out CoreInfo coreInfo)
    {
      try
      {
        string filename = Path.GetFileNameWithoutExtension(path);
        string text = File.ReadAllText(path);
        coreInfo = new CoreInfo(filename, text);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception loading info from '{0}'", ex, path);
      }
      coreInfo = null;
      return false;
    }

    protected bool TryCreateInfoDirectory(string infoDirectory)
    {
      try
      {
        Directory.CreateDirectory(infoDirectory);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception creating info directory '{0}'", ex, infoDirectory);
      }
      return false;
    }
  }
}