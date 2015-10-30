using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.WebRequests
{
  public abstract class AbstractDownloader
  {
    protected Encoding _encoding = Encoding.Default;

    public Encoding Encoding
    {
      get { return _encoding; }
      set { _encoding = value; }
    }

    public virtual T Download<T>(string url, string cachePath = null)
    {
      string responseString;
      if (TryGetCache(cachePath, out responseString))
        return Deserialize<T>(responseString);

      responseString = GetResponseString(url);
      T response = Deserialize<T>(responseString);
      WriteCache(cachePath, responseString);
      return response;
    }

    public virtual bool DownloadFile(string url, string downloadFile)
    {
      if (File.Exists(downloadFile))
        return true;
      try
      {
        WebClient webClient = new CompressionWebClient();
        webClient.Encoding = _encoding;
        webClient.DownloadFile(url, downloadFile);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception downloading file from '{0}' to '{1}' - {2}", url, downloadFile, ex);
        return false;
      }
    }

    protected abstract T Deserialize<T>(string response);

    protected virtual string GetResponseString(string url)
    {
      try
      {
        WebClient webClient = new CompressionWebClient();
        webClient.Encoding = _encoding;
        return webClient.DownloadString(url);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception getting response from '{0}' - {1}", url, ex);
      }
      return null;
    }

    protected virtual bool TryGetCache(string cachePath, out string cacheString)
    {
      cacheString = null;
      if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
        return false;
      cacheString = File.ReadAllText(cachePath);
      return true;
    }

    /// <summary>
    /// Writes XML strings to cache file.
    /// </summary>
    /// <param name="cachePath"></param>
    /// <param name="cacheString"></param>
    protected virtual void WriteCache(string cachePath, string cacheString)
    {
      if (string.IsNullOrEmpty(cachePath))
        return;

      using (FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
      {
        using (StreamWriter sw = new StreamWriter(fs))
        {
          sw.Write(cacheString);
          sw.Close();
        }
        fs.Close();
      }
    }
  }
}