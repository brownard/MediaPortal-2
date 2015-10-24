using Emulators.Common.GoodMerge;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.GoodMerge
{
  public class ExtractionCompletedEventArgs : EventArgs
  {
    public ExtractionCompletedEventArgs(string extractedItem, string extractedPath)
    {
      ExtractedItem = extractedItem;
      ExtractedPath = extractedPath;
    }
    public string ExtractedItem { get; private set; }
    public string ExtractedPath { get; private set; }
  }

  public class GoodMergeExtractor
  {
    protected const string EXTRACT_PATH_PREFIX = "MP2GoodMergeCache";
    protected IWork _extractionThread;

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public event EventHandler<ExtractionEventArgs> ExtractionProgress;
    protected virtual void OnExtractionProgress(object sender, ExtractionEventArgs e)
    {
      var handler = ExtractionProgress;
      if (handler != null)
        handler(this, e);
    }

    public event EventHandler<ExtractionCompletedEventArgs> ExtractionCompleted;
    protected virtual void OnExtractionCompleted(ExtractionCompletedEventArgs e)
    {
      var handler = ExtractionCompleted;
      if (handler != null)
        handler(this, e);
    }

    public void Extract(string archivePath, string selectedItem)
    {
      if (string.IsNullOrEmpty(archivePath) || string.IsNullOrEmpty(selectedItem))
        return;
      _extractionThread = ServiceRegistration.Get<IThreadPool>().Add(() => DoExtract(archivePath, selectedItem));
    }

    public void WaitForExtractionThread()
    {
      while (_extractionThread != null)
        Thread.Sleep(100);
    }

    protected void DoExtract(string archivePath, string selectedItem)
    {
      string extractionPath = GetExtractionPath(archivePath, selectedItem);
      Logger.Debug("GoodMergeExtractor: Extracting '{0}' from '{1}' to '{2}'", selectedItem, archivePath, extractionPath);
      string extractedPath;
      using (IExtractor extractor = ExtractorFactory.Create(archivePath))
      {
        extractor.ExtractionProgress += OnExtractionProgress;
        extractedPath = extractor.ExtractArchiveFile(selectedItem, extractionPath);
      }
      _extractionThread = null;
      OnExtractionCompleted(new ExtractionCompletedEventArgs(selectedItem, extractedPath));
    }

    public static bool IsExtracted(string archivePath, string selectedItem, out string extractedPath)
    {
      extractedPath = GetExtractionPath(archivePath, selectedItem);
      return File.Exists(extractedPath);
    }

    public static string GetExtractionDirectory()
    {
      return Path.Combine(Path.GetTempPath(), EXTRACT_PATH_PREFIX);
    }

    public static void DeleteExtractionDirectory()
    {
      string extractionDirectory = GetExtractionDirectory();
      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(extractionDirectory);
        if (directoryInfo.Exists)
          directoryInfo.Delete(true);
      }
      catch (Exception ex)
      {
        Logger.Warn("GoodMergeExtractor: Unable to delete extraction directory '{0}': {1}", extractionDirectory, ex);
      }
    }

    protected static string GetExtractionPath(string archivePath, string selectedItem)
    {
      return Path.Combine(GetExtractionDirectory(), GetPathHash(archivePath), selectedItem);
    }

    protected static string GetPathHash(string path)
    {
      byte[] input = Encoding.UTF8.GetBytes(path);
      byte[] output;
      using (MD5 md5Hash = MD5.Create())
        output = md5Hash.ComputeHash(input);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < output.Length; i++)
        sb.Append(output[i].ToString("x2"));
      return sb.ToString();
    }
  }
}
