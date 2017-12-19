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

using MediaPortal.Common.Certifications;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public abstract class INfoRelationshipExtractor
  {
    public static bool UpdatePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forSeries)
    {
      if (aspects.ContainsKey(TempActorAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempActorAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempActorAspect.ATTR_FROMSERIES) == forSeries)
            {
              PersonInfo info = infoPersons.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempActorAspect.ATTR_NAME), StringComparison.InvariantCultureIgnoreCase) &&
                  p.Occupation == person.GetAttributeValue<string>(TempActorAspect.ATTR_OCCUPATION) && string.IsNullOrEmpty(person.GetAttributeValue<string>(TempActorAspect.ATTR_CHARACTER)));
              if (info != null)
              {
                if(string.IsNullOrEmpty(info.ImdbId))
                  info.ImdbId = person.GetAttributeValue<string>(TempActorAspect.ATTR_IMDBID);
                if (info.Biography.IsEmpty)
                  info.Biography = person.GetAttributeValue<string>(TempActorAspect.ATTR_BIOGRAPHY);
                if (string.IsNullOrEmpty(info.Orign))
                  info.Orign = person.GetAttributeValue<string>(TempActorAspect.ATTR_ORIGIN);
                if (!info.DateOfBirth.HasValue)
                  info.DateOfBirth = person.GetAttributeValue<DateTime?>(TempActorAspect.ATTR_DATEOFBIRTH);
                if (!info.DateOfDeath.HasValue)
                  info.DateOfDeath = person.GetAttributeValue<DateTime?>(TempActorAspect.ATTR_DATEOFDEATH);
                if (!info.Order.HasValue)
                  info.Order = person.GetAttributeValue<int?>(TempActorAspect.ATTR_ORDER);
              }
            }
          }
          return true;
        }
      }
      return false;
    }

    public static void StorePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forSeries)
    {
      foreach (PersonInfo person in infoPersons)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
        personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, person.ImdbId);
        personAspect.SetAttribute(TempActorAspect.ATTR_NAME, person.Name);
        personAspect.SetAttribute(TempActorAspect.ATTR_OCCUPATION, person.Occupation);
        personAspect.SetAttribute(TempActorAspect.ATTR_BIOGRAPHY, person.Biography);
        personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFBIRTH, person.DateOfBirth);
        personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFDEATH, person.DateOfDeath);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, person.Order);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORIGIN, person.Orign);
        personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
      }
    }

    public static bool UpdateCharacters(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<CharacterInfo> infoCharacters, bool forSeries)
    {
      if (aspects.ContainsKey(TempActorAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempActorAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempActorAspect.ATTR_FROMSERIES) == forSeries)
            {
              CharacterInfo info = infoCharacters.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempActorAspect.ATTR_CHARACTER), StringComparison.InvariantCultureIgnoreCase));
              if (info != null)
              {
                if (string.IsNullOrEmpty(info.ActorImdbId))
                  info.ActorImdbId = person.GetAttributeValue<string>(TempActorAspect.ATTR_IMDBID);
                if (string.IsNullOrEmpty(info.ActorName))
                  info.ActorName = person.GetAttributeValue<string>(TempActorAspect.ATTR_NAME);
                  if (!info.Order.HasValue)
                  info.Order = person.GetAttributeValue<int?>(TempActorAspect.ATTR_ORDER);
              }
            }
          }
          return true;
        }
      }
      return false;
    }

    public static void StoreCharacters(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<CharacterInfo> infoCharacters, bool forSeries)
    {
      foreach (CharacterInfo character in infoCharacters)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
        personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, character.ActorImdbId);
        personAspect.SetAttribute(TempActorAspect.ATTR_NAME, character.ActorName);
        personAspect.SetAttribute(TempActorAspect.ATTR_CHARACTER, character.Name);
        personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, character.Order);
        personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
      }
    }

    public static void StorePersonAndCharacter(IDictionary<Guid, IList<MediaItemAspect>> aspects, PersonStub person, string occupation, bool forSeries)
    {
      MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempActorAspect.Metadata);
      personAspect.SetAttribute(TempActorAspect.ATTR_IMDBID, person.ImdbId);
      personAspect.SetAttribute(TempActorAspect.ATTR_NAME, person.Name);
      personAspect.SetAttribute(TempActorAspect.ATTR_OCCUPATION, occupation);
      personAspect.SetAttribute(TempActorAspect.ATTR_CHARACTER, person.Role);
      personAspect.SetAttribute(TempActorAspect.ATTR_BIOGRAPHY, !string.IsNullOrEmpty(person.Biography) ? person.Biography : person.MiniBiography);
      personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFBIRTH, person.Birthdate);
      personAspect.SetAttribute(TempActorAspect.ATTR_DATEOFDEATH, person.Deathdate);
      personAspect.SetAttribute(TempActorAspect.ATTR_ORDER, person.Order);
      personAspect.SetAttribute(TempActorAspect.ATTR_ORIGIN, person.Birthplace);
      personAspect.SetAttribute(TempActorAspect.ATTR_FROMSERIES, forSeries);
    }

    public static bool UpdateSeries(IDictionary<Guid, IList<MediaItemAspect>> aspects, SeriesInfo info)
    {
      if (aspects.ContainsKey(TempSeriesAspect.ASPECT_ID))
      {
        string text;
        double? number;
        DateTime? date;
        int? integer;
        bool? boolean;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_TVDBID, out integer) && integer.HasValue)
          info.TvdbId = integer.Value;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_NAME, out text) && !string.IsNullOrEmpty(text))
          info.SeriesName.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_SORT_NAME, out text) && !string.IsNullOrEmpty(text))
          info.SeriesNameSort.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_ENDED, out boolean) && boolean.HasValue)
          info.IsEnded = boolean.Value;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_PLOT, out text) && !string.IsNullOrEmpty(text))
          info.Description.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_CERTIFICATION, out text) && !string.IsNullOrEmpty(text))
          info.Certification = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_PREMIERED, out date) && date.HasValue)
          info.FirstAired = date;
        if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_RATING, out number) && number.HasValue)
        {
          info.Rating.RatingValue = number;
          info.Rating.VoteCount = null;
          if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_VOTES, out integer) && integer.HasValue)
            info.Rating.VoteCount = integer.Value;
        }

        if(info.Networks.Count == 0)
        {
          string station;
          if(MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_STATION, out station) && !string.IsNullOrEmpty(station))
            info.Networks.Add(new CompanyInfo { Name = station, Type = CompanyAspect.COMPANY_TV_NETWORK });
        }
        if(info.Genres.Count == 0)
        {
          IEnumerable collection;
          if (MediaItemAspect.TryGetAttribute(aspects, TempSeriesAspect.ATTR_GENRES, out collection))
            info.Genres.AddRange(collection.Cast<object>().Select(s => new GenreInfo { Name = s.ToString() }));
        }
        return true;
      }
      return false;
    }

    public static void StoreSeries(IDictionary<Guid, IList<MediaItemAspect>> aspects, SeriesStub series)
    {
      SingleMediaItemAspect seriesAspect = MediaItemAspect.GetOrCreateAspect(aspects, TempSeriesAspect.Metadata);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_TVDBID, series.Id.HasValue ? series.Id.Value : 0);
      string title = !string.IsNullOrEmpty(series.Title) ? series.Title : series.ShowTitle;
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_NAME, title);
      if(!string.IsNullOrEmpty(series.SortTitle))
        seriesAspect.SetAttribute(TempSeriesAspect.ATTR_SORT_NAME, series.SortTitle);
      else
        seriesAspect.SetAttribute(TempSeriesAspect.ATTR_SORT_NAME, BaseInfo.GetSortTitle(title));

      CertificationMapping cert = null;
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_CERTIFICATION, null);
      if (series.Mpaa != null && series.Mpaa.Any())
      {
        foreach (string certification in series.Mpaa)
        {
          if (CertificationMapper.TryFindSeriesCertification(certification, out cert))
          {
            seriesAspect.SetAttribute(TempSeriesAspect.ATTR_CERTIFICATION, cert.CertificationId);
            break;
          }
        }
      }
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_ENDED, !string.IsNullOrEmpty(series.Status) ? series.Status.Contains("End") : false);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_PLOT, !string.IsNullOrEmpty(series.Plot) ? series.Plot : series.Outline);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_PREMIERED, series.Premiered.HasValue ? series.Premiered.Value : series.Year.HasValue ? series.Year.Value : default(DateTime?));
      seriesAspect.SetCollectionAttribute(TempSeriesAspect.ATTR_GENRES, series.Genres);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_RATING, series.Rating.HasValue ? Convert.ToDouble(series.Rating.Value) : 0.0);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_VOTES, series.Votes.HasValue ? series.Votes.Value : series.Rating.HasValue ? 1 : 0);
      seriesAspect.SetAttribute(TempSeriesAspect.ATTR_STATION, series.Studio);
    }

    public static bool UpdateAlbum(IDictionary<Guid, IList<MediaItemAspect>> aspects, AlbumInfo info)
    {
      if (aspects.ContainsKey(TempAlbumAspect.ASPECT_ID))
      {
        string text;
        double? number;
        DateTime? date;
        long? integer;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_MBID, out text) && !string.IsNullOrEmpty(text))
          info.MusicBrainzId = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_MBGID, out text) && !string.IsNullOrEmpty(text))
          info.MusicBrainzGroupId = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_ADBID, out integer) && integer.HasValue)
          info.AudioDbId = integer.Value;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_NAME, out text) && !string.IsNullOrEmpty(text))
          info.Album = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_SORT_NAME, out text) && !string.IsNullOrEmpty(text))
          info.AlbumSort = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_REVIEW, out text) && !string.IsNullOrEmpty(text))
          info.Description.Text = text;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_RELEASEDATE, out date) && date.HasValue)
          info.ReleaseDate = date;
        if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_RATING, out number) && number.HasValue)
          info.Rating.RatingValue = number;

        if (info.Artists.Count == 0)
        {
          IEnumerable collection;
          if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_ARTISTS, out collection))
            info.Artists.AddRange(collection.Cast<object>().Select(s => new PersonInfo { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ARTIST }));
        }
        if (info.MusicLabels.Count == 0)
        {
          IEnumerable collection;
          if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_LABELS, out collection))
            info.MusicLabels.AddRange(collection.Cast<object>().Select(s => new CompanyInfo { Name = s.ToString(), Type = CompanyAspect.COMPANY_MUSIC_LABEL }));
        }
        if (info.Genres.Count == 0)
        {
          IEnumerable collection;
          if (MediaItemAspect.TryGetAttribute(aspects, TempAlbumAspect.ATTR_GENRES, out collection))
            info.Genres.AddRange(collection.Cast<object>().Select(s => new GenreInfo { Name = s.ToString() }));
        }
        return true;
      }
      return false;
    }

    public static void StoreAlbum(IDictionary<Guid, IList<MediaItemAspect>> aspects, AlbumStub album)
    {
      SingleMediaItemAspect albumAspect = MediaItemAspect.GetOrCreateAspect(aspects, TempAlbumAspect.Metadata);
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_ADBID, album.AudioDbId.HasValue ? album.AudioDbId.Value : 0);
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_MBID, !string.IsNullOrEmpty(album.MusicBrainzAlbumId) ? album.MusicBrainzAlbumId : null);
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_MBGID, !string.IsNullOrEmpty(album.MusicBrainzReleaseGroupId) ? album.MusicBrainzReleaseGroupId : null);
      if (!string.IsNullOrEmpty(album.Title))
      {
        albumAspect.SetAttribute(TempAlbumAspect.ATTR_NAME, album.Title);
        albumAspect.SetAttribute(TempAlbumAspect.ATTR_SORT_NAME, BaseInfo.GetSortTitle(album.Title));
      }
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_REVIEW, !string.IsNullOrEmpty(album.Review) ? album.Review : null);
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_RELEASEDATE, album.ReleaseDate.HasValue ? album.ReleaseDate.Value : album.Year.HasValue ? album.Year.Value : default(DateTime?));
      albumAspect.SetCollectionAttribute(TempAlbumAspect.ATTR_GENRES, album.Genres);
      albumAspect.SetCollectionAttribute(TempAlbumAspect.ATTR_ARTISTS, album.Artists);
      albumAspect.SetCollectionAttribute(TempAlbumAspect.ATTR_LABELS, album.Labels);
      albumAspect.SetAttribute(TempAlbumAspect.ATTR_RATING, album.Rating.HasValue ? Convert.ToDouble(album.Rating.Value) : 0.0);
    }

    public static bool UpdateArtists(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forAlbum)
    {
      if (aspects.ContainsKey(TempArtistAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempArtistAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempArtistAspect.ATTR_FROMALBUM) == forAlbum)
            {
              PersonInfo info = infoPersons.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempArtistAspect.ATTR_NAME), StringComparison.InvariantCultureIgnoreCase) &&
                  p.Occupation == person.GetAttributeValue<string>(TempArtistAspect.ATTR_OCCUPATION));
              if (info != null)
              {
                if (info.AudioDbId <= 0)
                  info.AudioDbId = person.GetAttributeValue<long>(TempArtistAspect.ATTR_ADBID);
                if (string.IsNullOrEmpty(info.MusicBrainzId))
                  info.MusicBrainzId = person.GetAttributeValue<string>(TempArtistAspect.ATTR_MBID);
                if (info.Biography.IsEmpty)
                  info.Biography = person.GetAttributeValue<string>(TempArtistAspect.ATTR_BIOGRAPHY);
                if (!info.DateOfBirth.HasValue)
                  info.DateOfBirth = person.GetAttributeValue<DateTime?>(TempArtistAspect.ATTR_DATEOFBIRTH);
                if (!info.DateOfDeath.HasValue)
                  info.DateOfDeath = person.GetAttributeValue<DateTime?>(TempArtistAspect.ATTR_DATEOFDEATH);
                info.IsGroup = person.GetAttributeValue<bool>(TempArtistAspect.ATTR_GROUP);
              }
            }
          }
          return true;
        }
      }
      return false;
    }

    public static void StoreArtist(IDictionary<Guid, IList<MediaItemAspect>> aspects, ArtistStub artist, bool forAlbum)
    {
      MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempArtistAspect.Metadata);
      personAspect.SetAttribute(TempArtistAspect.ATTR_ADBID, artist.AudioDbId);
      personAspect.SetAttribute(TempArtistAspect.ATTR_MBID, artist.MusicBrainzArtistId);
      personAspect.SetAttribute(TempArtistAspect.ATTR_NAME, artist.Name);
      personAspect.SetAttribute(TempArtistAspect.ATTR_OCCUPATION, PersonAspect.OCCUPATION_ARTIST);
      personAspect.SetAttribute(TempArtistAspect.ATTR_BIOGRAPHY, artist.Biography);
      if (artist.Birthdate.HasValue || artist.Deathdate.HasValue)
      {
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFBIRTH, artist.Birthdate);
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFDEATH, artist.Deathdate);
        personAspect.SetAttribute(TempArtistAspect.ATTR_GROUP, false);
      }
      else
      {
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFBIRTH, artist.Formeddate);
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFDEATH, artist.Disbandeddate);
        personAspect.SetAttribute(TempArtistAspect.ATTR_GROUP, true);
      }
      personAspect.SetAttribute(TempArtistAspect.ATTR_FROMALBUM, forAlbum);
    }

    public static void StoreArtists(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forAlbum)
    {
      foreach (PersonInfo person in infoPersons)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempArtistAspect.Metadata);
        personAspect.SetAttribute(TempArtistAspect.ATTR_ADBID, person.AudioDbId);
        personAspect.SetAttribute(TempArtistAspect.ATTR_MBID, person.MusicBrainzId);
        personAspect.SetAttribute(TempArtistAspect.ATTR_NAME, person.Name);
        personAspect.SetAttribute(TempArtistAspect.ATTR_OCCUPATION, person.Occupation);
        personAspect.SetAttribute(TempArtistAspect.ATTR_BIOGRAPHY, person.Biography.Text);
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFBIRTH, person.DateOfBirth);
        personAspect.SetAttribute(TempArtistAspect.ATTR_DATEOFDEATH, person.DateOfDeath);
        personAspect.SetAttribute(TempArtistAspect.ATTR_GROUP, person.IsGroup);
        personAspect.SetAttribute(TempArtistAspect.ATTR_FROMALBUM, forAlbum);
      }
    }
  }
}
