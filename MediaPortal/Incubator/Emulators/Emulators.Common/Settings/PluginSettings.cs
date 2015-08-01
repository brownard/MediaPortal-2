using Emulators.Common.TheGamesDb;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Settings
{
  public class PluginSettings
  {
    List<string> configuredPlatforms = TheGamesDbWrapper.Platforms.Select(p => p.Name).ToList();
    [Setting(SettingScope.Global)]
    public List<string> ConfiguredPlatforms
    {
      get { return configuredPlatforms; }
      set { configuredPlatforms = value; }
    }
  }
}
