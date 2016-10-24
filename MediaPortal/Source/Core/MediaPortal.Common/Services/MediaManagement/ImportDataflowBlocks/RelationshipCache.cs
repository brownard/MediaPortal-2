#region Copyright (C) 2007-2017 Team MediaPortal

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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  class RelationshipCache
  {    
    protected IDictionary<string, IList<MediaItem>> _externalItemsMatch;

    public RelationshipCache()
    {
      _externalItemsMatch = new Dictionary<string, IList<MediaItem>>();
    }

    public bool TryAddExternalItem(MediaItem item)
    {
      IList<MultipleMediaItemAspect> externalAspects;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, ExternalIdentifierAspect.Metadata, out externalAspects) || externalAspects.Count == 0)
        return false;

      bool itemAdded = false;
      MediaItem cached = item;
      foreach (MultipleMediaItemAspect externalAspect in externalAspects)
      {
        string cacheKey = GetCacheKey(externalAspect);
        IList<MediaItem> cacheList;
        if (!_externalItemsMatch.TryGetValue(cacheKey, out cacheList))
          _externalItemsMatch[cacheKey] = cacheList = new List<MediaItem>();
        else if (cacheList.Any(i => i.MediaItemId == item.MediaItemId))
          continue;

        cacheList.Add(item);
        itemAdded = true;
      }
      return itemAdded;
    }

    public bool TryGetExternalItem(IDictionary<Guid, IList<MediaItemAspect>> extractedItem, IRelationshipRoleExtractor externalItemMatcher, out MediaItem externalItem)
    {
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedItem, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        ICollection<Guid> checkedIds = new HashSet<Guid>();
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string cacheKey = GetCacheKey(externalAspect);
          IList<MediaItem> cacheList;
          if (!_externalItemsMatch.TryGetValue(cacheKey, out cacheList))
            continue;

          foreach (MediaItem item in cacheList)
          {
            if (checkedIds.Contains(item.MediaItemId))
              continue;
            if (externalItemMatcher.TryMatch(extractedItem, item.Aspects))
            {
              externalItem = item;
              return true;
            }
            checkedIds.Add(item.MediaItemId);
          }
        }
      }
      externalItem = null;
      return false;
    }

    protected static string GetCacheKey(MultipleMediaItemAspect externalAspect)
    {
      string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
      string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
      string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
      return string.Format("{0} | {1} | {2}", source, type, id);
    }
  }
}
