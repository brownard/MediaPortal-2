using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb
{
  public class TheGamesDbSettings
  {
    [Setting(SettingScope.Global, 0)]
    public int APIVersion { get; set; }
  }
}
