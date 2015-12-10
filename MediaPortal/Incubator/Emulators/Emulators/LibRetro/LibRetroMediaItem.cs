using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroMediaItem : MediaItem
  {
    public LibRetroMediaItem(IDictionary<Guid, MediaItemAspect> aspects)
      : base(Guid.Empty, aspects)
    {
      //Aspects[ProviderResourceAspect.ASPECT_ID].SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
      //otherwise MP2's player manager won't try and find a player 
      if (!Aspects.ContainsKey(VideoAspect.Metadata.AspectId))
        Aspects[VideoAspect.Metadata.AspectId] = new MediaItemAspect(VideoAspect.Metadata);
    }

    public bool TryGetPlatform(out string platform)
    {
      return MediaItemAspect.TryGetAttribute(Aspects, Common.Games.GameAspect.ATTR_PLATFORM, out platform);
    }
  }
}
