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
    
    protected List<LocalCore> _cores;
    protected HtmlDownloader _downloader;

    public CoreHandler()
    {
      _downloader = new HtmlDownloader();
      _cores = new List<LocalCore>();
    }

    public List<LocalCore> Cores
    {
      get { return _cores; }
    }

    public void Update()
    {
      CoreList coreList = _downloader.Download<CoreList>(BASE_URL + LATEST_URL);
      if (coreList != null)
        CreateCoresList(coreList.CoreUrls);
    }

    public bool DownloadCore(LocalCore core, string coresDirectory)
    {
      if (!TryCreateCoresDirectory(coresDirectory))
        return false;

      string path = Path.Combine(coresDirectory, core.ArchiveName);
      return _downloader.DownloadFile(core.Url, path, true) && ExtractCore(path, coresDirectory);
    }

    protected bool ExtractCore(string path, string coresDirectory)
    {
      bool extracted;
      using (IExtractor extractor = ExtractorFactory.Create(path))
      {
        if (!extractor.IsArchive())
          return true;
        extracted = extractor.ExtractAll(coresDirectory);
      }
      if (extracted)
        TryDeleteFile(path);
      return extracted;
    }

    protected void CreateCoresList(IEnumerable<OnlineCore> onlineCores)
    {
      _cores = new List<LocalCore>();
      foreach (OnlineCore onlineCore in onlineCores)
      {
        Uri uri;
        if (!Uri.TryCreate(onlineCore.Url, UriKind.RelativeOrAbsolute, out uri))
          continue;
        if (!uri.IsAbsoluteUri)
          uri = new Uri(new Uri(BASE_URL), uri);

        LocalCore core = new LocalCore()
        {
          Url = uri.AbsoluteUri,
          ArchiveName = onlineCore.Name,
          CoreName = Path.GetFileNameWithoutExtension(onlineCore.Name)
        };
        _cores.Add(core);
      }
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