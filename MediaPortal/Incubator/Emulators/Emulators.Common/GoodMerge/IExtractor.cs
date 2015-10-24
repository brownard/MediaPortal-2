﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.GoodMerge
{
  public class ExtractionEventArgs : EventArgs
  {
    public ExtractionEventArgs(int percent)
    {
      Percent = percent;
    }

    public int Percent { get; private set; }
  }

  public interface IExtractor : IDisposable
  {
    event EventHandler<ExtractionEventArgs> ExtractionProgress;
    event EventHandler ExtractionComplete;
    List<string> GetArchiveFiles();
    string ExtractArchiveFile(string archiveFile, string extractionDir);
    bool IsArchive();
  }
}
