using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.GoodMerge
{
  public static class GoodMergeAspect
  {
    public static readonly Guid ASPECT_ID = new Guid("799B6A01-07B2-4E29-A7E3-F3EBF251A916");

    /// <summary>
    /// Contains the last played goodmerge item.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_LAST_PLAYED_ITEM =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("LastPlayedItem", 100, Cardinality.Inline, false);

    /// <summary>
    /// Goodmerge items.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_GOODMERGE_ITEMS =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("GoodMergeItems", 255, Cardinality.ManyToMany, false);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "GoodMergeItem", new[] {
            ATTR_LAST_PLAYED_ITEM,
            ATTR_GOODMERGE_ITEMS
        });
  }
}
