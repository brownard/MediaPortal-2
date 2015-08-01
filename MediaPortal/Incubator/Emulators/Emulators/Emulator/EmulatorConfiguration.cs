using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Emulator
{
    public class EmulatorConfiguration
    {
        public string Name { get; set; }
        public List<EmulatorFilter> Filters { get; set; }
        public string Path { get; set; }
        public string WorkingDirectory { get; set; }
        public string Arguments { get; set; }
        public bool UseQuotes { get; set; }
    }
}
