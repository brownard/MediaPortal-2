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

namespace Emulators.Common.WebRequests
{
  public class Downloader
  {
    protected Encoding _encoding = Encoding.Default;

    public Encoding Encoding
    {
      get { return _encoding; }
      set { _encoding = value; }
    }

    public T Download<T>(string url, string cachePath = null)
    {
      string xml;
      if(TryGetCache(cachePath, out xml))
        return Deserialize<T>(xml);

      xml = GetXml(url);
      T response = Deserialize<T>(xml);
      WriteCache(cachePath, xml);
      return response;
    }

    public bool DownloadFile(string url, string downloadFile)
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

    public T Deserialize<T>(string response)
    {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (XmlReader reader = XmlReader.Create(new StringReader(response)))
        return (T)serializer.Deserialize(reader);
    }

    protected string GetXml(string url)
    {
      try
      {
        WebClient webClient = new CompressionWebClient();
        webClient.Encoding = _encoding;
        return webClient.DownloadString(url);
      }
      catch(Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception getting response from '{0}' - {1}", url, ex);
      }
      return null;
    }

    protected bool TryGetCache(string cachePath, out string xml)
    {
      xml = null;
      if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
        return false;
      xml = File.ReadAllText(cachePath);
      return true;      
    }
    
    /// <summary>
     /// Writes XML strings to cache file.
     /// </summary>
     /// <param name="cachePath"></param>
     /// <param name="xml"></param>
    protected void WriteCache(string cachePath, string xml)
    {
      if (string.IsNullOrEmpty(cachePath))
        return;

      using (FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
      {
        using (StreamWriter sw = new StreamWriter(fs))
        {
          sw.Write(xml);
          sw.Close();
        }
        fs.Close();
      }
    }
  }
}
