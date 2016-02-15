using Emulators.LibRetro.Render;
using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class SyncSetting : SingleSelectionList
  {
    protected static readonly IList<SynchronizationType> SYNC_TYPES = new List<SynchronizationType> { SynchronizationType.Audio, SynchronizationType.VSync };
    public override void Load()
    {
      List<string> options = new List<string>();
      options.Add("[Emulators.Config.LibRetro.SyncStrategy.Audio]");
      options.Add("[Emulators.Config.LibRetro.SyncStrategy.VSync]");
      _items = options.Select(LocalizationHelper.CreateResourceString).ToList();
      Selected = SYNC_TYPES.IndexOf(SettingsManager.Load<LibRetroSettings>().SynchronizationType);
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.SynchronizationType = SYNC_TYPES[Selected];
      SettingsManager.Save(settings);
    }
  }
}
