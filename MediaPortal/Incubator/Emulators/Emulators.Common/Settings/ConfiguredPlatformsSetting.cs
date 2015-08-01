using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Settings
{
  public class ConfiguredPlatformsSetting : CustomConfigSetting
  {
    public List<string> Platforms { get; set; }

    public override void Load()
    {
      Platforms = SettingsManager.Load<PluginSettings>().ConfiguredPlatforms;
      base.Load();
    }

    public override void Save()
    {
      PluginSettings settings = SettingsManager.Load<PluginSettings>();
      settings.ConfiguredPlatforms = Platforms;
      SettingsManager.Save(settings);
      base.Save();
    }
  }
}
