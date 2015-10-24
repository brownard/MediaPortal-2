using MediaPortal.UiComponents.Media.MediaItemActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using Emulators.Common.GoodMerge;
using Emulators.Models;
using MediaPortal.Common;
using Emulators.Game;

namespace Emulators.MediaExtensions.MediaItemActions
{
  public class SelectGoodMergeAction : AbstractMediaItemAction
  {
    public override bool IsAvailable(MediaItem mediaItem)
    {
      IEnumerable<string> goodMergeItems;
      return MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodMergeItems) && goodMergeItems != null;
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;
      IEnumerable<string> goodMergeItems;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodMergeItems))
        return false;
      MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, null);
      ServiceRegistration.Get<IGameLauncher>().LaunchGame(mediaItem);
      return true;
    }
  }
}
