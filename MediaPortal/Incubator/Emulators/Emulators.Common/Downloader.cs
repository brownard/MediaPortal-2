using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Common
{
  class Downloader
  {
    DownloadCache _cache = new DownloadCache();

    public T Download<T>(string url)
    {
      byte[] response = getResponse(url);
      if (response != null && response.Length > 0)
        return deserialize<T>(response);
      return default(T);
    }

    public bool DownloadFile(string url, string downloadFile)
    {
      if (File.Exists(downloadFile))
        return true;
      try
      {
        WebClient webClient = new CompressionWebClient();
        webClient.DownloadFile(url, downloadFile);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception downloading file from '{0}' to '{1}' - {2}", url, downloadFile, ex);
        return false;
      }
    }

    T deserialize<T>(byte[] data)
    {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (XmlReader reader = XmlReader.Create(new MemoryStream(data)))
        return (T)serializer.Deserialize(reader);
    }

    byte[] getResponse(string url)
    {
      byte[] response;
      if (_cache.TryGetFromCache(url, out response))
        return response;

      try
      {
        WebClient webClient = new CompressionWebClient();
        return webClient.DownloadData(url);
      }
      catch(Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception getting response from '{0}' - {1}", url, ex);
      }
      return null;
    }
  }
}
