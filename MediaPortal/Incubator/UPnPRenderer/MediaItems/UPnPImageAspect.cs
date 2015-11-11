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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UPnPRenderer.MediaItems
{
  /// <summary>
  /// Contains the metadata specification for images sent via UPnP.
  /// </summary>
  public static class UPnPImageAspect
  {
    /// <summary>
    /// Media item aspect id of the large thumbnail aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("2062636D-ECA1-4259-A7E0-CA1E406914D6");

    /// <summary>
    /// Contains the image byte data.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_IMAGE =
        MediaItemAspectMetadata.CreateAttributeSpecification("ImageBinary", typeof(byte[]), Cardinality.Inline, false);

    /// <summary>
    /// Image ID.
    /// </summary>
    public static readonly MediaItemAspectMetadata.AttributeSpecification ATTR_IMAGE_ID =
        MediaItemAspectMetadata.CreateStringAttributeSpecification("ImageId", 50, Cardinality.Inline, true);

    public static readonly MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "UPnPImageAspect", new[] {
            ATTR_IMAGE,
            ATTR_IMAGE_ID,
        });
  }
}
