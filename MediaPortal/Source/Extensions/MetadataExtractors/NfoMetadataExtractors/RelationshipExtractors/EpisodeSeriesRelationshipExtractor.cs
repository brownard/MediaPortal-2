﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using MediaPortal.Common;
using MediaPortal.Common.Genres;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class EpisodeSeriesRelationshipExtractor : INfoRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return EpisodeAspect.ROLE_EPISODE; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return SeriesAspect.ROLE_SERIES; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return SeriesInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      //Only run during import
      if (!importOnly)
        return false;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(aspects))
        return false;

      SeriesInfo seriesInfo = episodeInfo.CloneBasicInstance<SeriesInfo>();
      UpdatePersons(aspects, seriesInfo.Actors, true);
      UpdateCharacters(aspects, seriesInfo.Characters, true);
      if (!UpdateSeries(aspects, seriesInfo))
        return false;
      GenreMapper.AssignMissingSeriesGenreIds(seriesInfo.Genres);

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      seriesInfo.SetMetadata(seriesAspects);

      if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        bool episodeVirtual = true;
        if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
        {
          MediaItemAspect.SetAttribute(seriesAspects, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);
        }
      }

      if (!seriesAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      StorePersons(seriesAspects, seriesInfo.Actors, true);
      StoreCharacters(seriesAspects, seriesInfo.Characters, true);

      extractedLinkedAspects.Add(seriesAspects);
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(SeriesAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(aspects, EpisodeAspect.Metadata, out linkedAspect))
        return false;

      int? season = linkedAspect.GetAttributeValue<int?>(EpisodeAspect.ATTR_SEASON);
      if (!season.HasValue)
        return false;

      IEnumerable<int> episodes = linkedAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      List<int> episodeList = new SafeList<int>(episodes);

      index = season.Value * 1000 + episodeList.First();
      return index >= 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
