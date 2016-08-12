#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  public interface IMediaItemAspectTypeRegistration
  {
    /// <summary>
    /// Returns all media item types which were registered in this registration instance.
    /// </summary>
    /// <value>Mapping of aspect type ids to aspect types.</value>
    IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes { get; }

    /// <summary>
    /// Registration method for all media item aspect types which are known by the local system.
    /// Each module, which brings in new media item aspect types, must register them at each system start
    /// (or at least before working with them).
    /// </summary>
    /// <remarks>
    /// This method will store media item aspect types which are not registered yet; others, which were already
    /// registered before, are ignored. It will also register the aspect types at the media portal server.
    /// It is needed to register a media item aspect type 1) before the local importer can send media item
    /// data of that type (extracted by a metadata extractor) to the MediaPortal server and 2) for the deserialization
    /// system.
    /// </remarks>
    /// <param name="miaType">Media item aspect type to register.</param>
    void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType);

    /// <summary>
    /// Registration method for all media item aspect types which are known by the local system.
    /// Each module, which brings in new media item aspect types, must register them at each system start
    /// (or at least before working with them). The dependency on an existing aspect will allow items of this aspect
    /// to be automatically deleted when the referenced aspect item is deleted.
    /// </summary>
    /// <remarks>
    /// This method will store media item aspect types which are not registered yet; others, which were already
    /// registered before, are ignored. It will also register the aspect types at the media portal server.
    /// It is needed to register a media item aspect type 1) before the local importer can send media item
    /// data of that type (extracted by a metadata extractor) to the MediaPortal server and 2) for the deserialization
    /// system.
    /// </remarks>
    /// <param name="miaType">Media item aspect type to register.</param>
    /// <param name="fkSpecs">Media item aspect type attributes that reference foreign attributes.</param>
    /// <param name="refType">Media item aspect type referenced by this media item aspect type.</param>
    /// <param name="refSpecs">Media item aspect type attributes referenced by this media item aspect types attributes.</param>
    void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miaType, MediaItemAspectMetadata.AttributeSpecification[] fkSpecs, MediaItemAspectMetadata refType, MediaItemAspectMetadata.AttributeSpecification[] refSpecs);

    /// <summary>
    /// Registration method for all media item aspect types which have roles that are in a hierarchy. Specifies a 
    /// relationship between child role and parent role.
    /// </summary>
    /// <remarks>
    /// This method will be used to delete virtual media items that are pure virtual, meaning no physical resource is attached 
    /// to any media item in the hierarchy.
    /// </remarks>
    /// <param name="childRole">The role of the child.</param>
    /// <param name="parentRole">The role of the parent.</param>
    void RegisterMediaItemAspectRoleHierarchy(Guid childRole, Guid parentRole);

    /// <summary>
    /// Registration method for all media item aspect types which have roles that are in a hierarchy. Specifies an 
    /// attribute that contains the number of child roles are available for the parent role.
    /// </summary>
    /// <remarks>
    /// This method will be used to delete virtual media items that are pure virtual, meaning no physical resource is attached 
    /// to any media item in the hierarchy.
    /// </remarks>
    /// <param name="childRole">The role of the child.</param>
    /// <param name="parentRole">The role of the parent.</param>
    /// <param name="parentMiaType">Media item aspect type of the parent.</param>
    /// <param name="childCountAttribute">Media item aspect type attributes that reference foreign attributes.</param>
    /// <param name="includeVirtual">Includes virtual child in the count.</param>
    void RegisterMediaItemAspectRoleHierarchyChildCountAttribute(Guid childRole, Guid parentRole, MediaItemAspectMetadata parentMiaType, 
      MediaItemAspectMetadata.AttributeSpecification childCountAttribute, bool includeVirtual);
  }
}
