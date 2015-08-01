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

    /// <summary>
    /// Contains the localized name of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_GAME_NAME =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("GameName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the TGDB ID of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_TGDB_ID =
        MediaItemAspectMetadata.CreateAttributeSpecification("TGDBID", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the platform of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_PLATFORM =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Platform", 100, Cardinality.Inline, false);
    
    /// <summary>
    /// Contains the release year of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_YEAR =
        MediaItemAspectMetadata.CreateAttributeSpecification("Year", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// Contains the description of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_DESCRIPTION =
      MediaItemAspectMetadata.CreateStringAttributeSpecification("Description", 10000, Cardinality.Inline, false);

    /// <summary>
    /// Contains the certification.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_CERTIFICATION =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Certification", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the developer of the game.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_DEVELOPER =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Developer", 100, Cardinality.Inline, false);

    /// <summary>
    /// Contains the overall rating of the movie. Value ranges from 0 (very bad) to 10 (very good).
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_RATING =
        MediaItemAspectMetadata.CreateAttributeSpecification("Rating", typeof(double), Cardinality.Inline, true);

    /// <summary>
    /// Genre string.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_GENRES =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("Genres", 100, Cardinality.ManyToMany, true);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
      // TODO: Localize name
      ASPECT_ID, "GameItem", new[] {
            ATTR_GAME_NAME,
            ATTR_TGDB_ID,
            ATTR_PLATFORM,
            ATTR_YEAR,
            ATTR_DESCRIPTION,
            ATTR_CERTIFICATION,
            ATTR_DEVELOPER,
            ATTR_RATING,
            ATTR_GENRES
        });
  }
}
