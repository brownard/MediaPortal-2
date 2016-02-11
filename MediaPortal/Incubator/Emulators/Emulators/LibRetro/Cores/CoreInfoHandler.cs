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

    protected string _infoDirectory;
    protected HtmlDownloader _downloader;
    protected List<CoreInfo> _coreInfos;

    public CoreInfoHandler(string infoDirectory)
    {
      _infoDirectory = infoDirectory;
      _downloader = new HtmlDownloader();
      _coreInfos = new List<CoreInfo>();
    }

    public List<CoreInfo> CoreInfos
    {
      get { return _coreInfos; }
    }

    public void Update()
    {
      DownloadCoreInfos();
      LoadCoreInfos();
    }

    public void Load()
    {
      LoadCoreInfos();
    }

    protected void DownloadCoreInfos()
    {
      if (!TryCreateInfoDirectory())
        return;

      CoreInfoList infoList = _downloader.Download<CoreInfoList>(BASE_URL + INFO_URL);
      if (infoList == null)
        return;

      foreach (string infoUrl in infoList.CoreInfoUrls)
      {
        Uri uri;
        if (!Uri.TryCreate(infoUrl, UriKind.RelativeOrAbsolute, out uri))
          continue;
        string url = uri.IsAbsoluteUri ? infoUrl : BASE_URL + infoUrl;
        string path = Path.Combine(_infoDirectory, Path.GetFileName(uri.LocalPath));
        _downloader.DownloadFile(url, path, true);
      }
    }

    protected void LoadCoreInfos()
    {
      _coreInfos = new List<CoreInfo>();
      if (!Directory.Exists(_infoDirectory))
        return;

      try
      {
        foreach (string file in Directory.EnumerateFiles(_infoDirectory, "*.info"))
        {
          CoreInfo coreInfo;
          if (TryLoadCoreInfo(file, out coreInfo))
            _coreInfos.Add(coreInfo);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception reading directory '{0}'", ex, _infoDirectory);
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

    protected bool TryCreateInfoDirectory()
    {
      try
      {
        Directory.CreateDirectory(_infoDirectory);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreInfoHandler: Exception creating info directory '{0}'", ex, _infoDirectory);
      }
      return false;
    }
  }
}