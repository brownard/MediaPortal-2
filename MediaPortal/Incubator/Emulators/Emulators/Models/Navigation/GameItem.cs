using Emulators.Common.Settings;
using Emulators.Emulator;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models.Navigation
{
  class GameItem : PlayableMediaItem
  {
    protected MediaItem _mediaItem;

    public GameItem(MediaItem mediaItem)
      : base(mediaItem)
    { }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      SimpleTitle = Title;
      var sm = ServiceRegistration.Get<IServerSettingsClient>();
      PluginSettings settings = sm.Load<PluginSettings>();
      settings.ConfiguredPlatforms = new List<string>(new[] { "Nintendo 64" });
      sm.Save(settings);
      //new EmulatorConfigurationManager().Serialize();
    }
  }
}
