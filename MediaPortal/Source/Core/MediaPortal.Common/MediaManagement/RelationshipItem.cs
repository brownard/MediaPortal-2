#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
  /// <summary>
  /// Encapsulates a relationship extracted from a <see cref="IRelationshipRoleExtractor"/>.
  /// </summary>
  public class RelationshipItem
  {
    /// <summary>
    /// Creates a new <see cref="RelationshipItem"/>.
    /// </summary>
    /// <param name="aspects">The extracted aspects of the relationship item.</param>
    /// <param name="role">The role of the media item that this relationship belongs to.</param>
    /// <param name="linkedRole">The role of this relationship.</param>
    public RelationshipItem(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid role, Guid linkedRole)
    {
      Aspects = aspects;
      Role = role;
      LinkedRole = linkedRole;
    }

    /// <summary>
    /// The extracted aspects of the relationship item.
    /// </summary>
    public IDictionary<Guid, IList<MediaItemAspect>> Aspects { get; set; }

    /// <summary>
    /// The role of the aspects this relationship was extracted from.
    /// </summary>
    public Guid Role { get; set; }

    /// <summary>
    /// The role of the aspects of this relationship.
    /// </summary>
    public Guid LinkedRole { get; set; }
  }
}
