using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Settings
{
  public class LibRetroSettings
  {
    [Setting(SettingScope.User, 4)]
    public int MaxPlayers { get; set; }

    [Setting(SettingScope.User, true)]
    public bool AutoSave { get; set; }

    [Setting(SettingScope.User, 30)]
    public int AutoSaveInterval { get; set; }
  }
}
