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
    public LibRetroMediaItem(string libRetroPath, IDictionary<Guid, MediaItemAspect> aspects)
      : base(Guid.Empty, aspects)
    {
      LibRetroPath = libRetroPath;
      //otherwise MP2's player manager won't try and find a player 
      if (!Aspects.ContainsKey(VideoAspect.Metadata.AspectId))
        Aspects[VideoAspect.Metadata.AspectId] = new MediaItemAspect(VideoAspect.Metadata);
    }

    public string LibRetroPath { get; set; }
    public string ExtractedPath { get; set; }
  }
}
