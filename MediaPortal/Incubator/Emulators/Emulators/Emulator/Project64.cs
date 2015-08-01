using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Emulator
{
  public class Project64 : EmulatorConfiguration
  {
    public static EmulatorConfiguration Create()
    {
      return new EmulatorConfiguration()
      {
        Path = @"C:\Program Files (x86)\Project64 2.1\Project64.exe",
        UseQuotes = true,
        Filters = new List<EmulatorFilter>(new[] { new EmulatorFilter() { MimeType = "game/nintendo-64" } }) //{ Path = "/D:/Games/" } })
      };
    }

    public Project64()
    {
      Path = @"C:\Program Files (x86)\Project64 2.1\Project64.exe";
      Arguments = "\"<gamepath>\"";
      WorkingDirectory = @"C:\Program Files (x86)\Project64 2.1";
      Filters = new List<EmulatorFilter>();
      Filters.Add(new EmulatorFilter() { Path = "/C:/ffmpeg/" });
    }
  }
}
