using Emulators.Common.GoodMerge;
using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  public class CoreHandler
  {
    protected const string BASE_URL = "http://buildbot.libretro.com";
    protected const string LATEST_URL = "/nightly/windows/x86/latest/";
    
    protected List<CoreUrl> _onlineCores;
    protected HtmlDownloader _downloader;

    public CoreHandler()
    {
      _downloader = new HtmlDownloader();
      _onlineCores = new List<CoreUrl>();
    }

    public List<CoreUrl> OnlineCores
    {
      get { return _onlineCores; }
    }

    public void Update()
    {
      CoreList coreList = _downloader.Download<CoreList>(BASE_URL + LATEST_URL);
      if (coreList != null)
        _onlineCores = coreList.CoreUrls;
    }

    public void DownloadCore(CoreUrl coreUrl, string coresDirectory)
    {
      if (!TryCreateCoresDirectory(coresDirectory))
        return;

      Uri uri;
      if (!Uri.TryCreate(coreUrl.Url, UriKind.RelativeOrAbsolute, out uri))
        return;

      string url = uri.IsAbsoluteUri ? coreUrl.Url : BASE_URL + coreUrl.Url;
      string path = Path.Combine(coresDirectory, coreUrl.Name);
      _downloader.DownloadFile(url, path, true);
      ExtractCore(path, coresDirectory);
    }

    protected void ExtractCore(string path, string coresDirectory)
    {
      using (IExtractor extractor = ExtractorFactory.Create(path))
      {
        if (!extractor.IsArchive() || !extractor.ExtractAll(coresDirectory))
          return;
      }
      TryDeleteFile(path);
    }

    protected bool TryCreateCoresDirectory(string coresDirectory)
    {
      try
      {
        Directory.CreateDirectory(coresDirectory);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreHandler: Exception creating cores directory '{0}'", ex, coresDirectory);
      }
      return false;
    }

    protected bool TryDeleteFile(string path)
    {
      try
      {
        File.Delete(path);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CoreHandler: Exception deleting file '{0}'", ex, path);
      }
      return false;
    }
  }
}