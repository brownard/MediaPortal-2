using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
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
    }
  }
}
