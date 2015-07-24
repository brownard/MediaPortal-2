using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public static class GameAspect
  {
    public static readonly Guid ASPECT_ID = new Guid("71D500E8-F2C3-4DAF-8CE6-A89DFE8FD96E");

    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_PLATFORM_ID =
      MediaItemAspectMetadata.CreateAttributeSpecification("PlatformId", typeof(int), Cardinality.Inline, true);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "GameItem", new[] {
            ATTR_PLATFORM_ID
        });
  }
}
