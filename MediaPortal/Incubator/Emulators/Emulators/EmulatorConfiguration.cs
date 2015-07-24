using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators
{
  public class EmulatorConfiguration
  {
    public string Path { get; set; }
    public string Arguments { get; set; }
    public string WorkingDirectory { get; set; }
  }

  class PJ64Config : EmulatorConfiguration
  {
    public PJ64Config()
    {
      Path = @"C:\Program Files (x86)\Project64 2.1\Project64.exe";
      Arguments = "\"<gamepath>\"";
      WorkingDirectory = @"C:\Program Files (x86)\Project64 2.1";
    }
  }
}
