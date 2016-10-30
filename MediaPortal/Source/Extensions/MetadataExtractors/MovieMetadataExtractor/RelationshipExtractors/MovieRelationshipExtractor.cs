﻿#region Copyright (C) 2007-2015 Team MediaPortal

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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using System.Threading;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieRelationshipExtractor : IRelationshipExtractor, IDisposable
  {
    #region Constants

    /// <summary>
    /// GUID string for the movie relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "BA7708DA-010E-45E1-A2B3-24B7D43443AC";

    /// <summary>
    /// Series relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    protected AsynchronousMessageQueue _messageQueue;
    protected int _importerCount;
    private IList<IRelationshipRoleExtractor> _extractors;

    public MovieRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Movie relationship extractor");

      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new MovieCollectionRelationshipExtractor());
      _extractors.Add(new MovieActorRelationshipExtractor());
      _extractors.Add(new MovieDirectorRelationshipExtractor());
      _extractors.Add(new MovieWriterRelationshipExtractor());
      _extractors.Add(new MovieCharacterRelationshipExtractor());
      _extractors.Add(new MovieProductionRelationshipExtractor());
      _extractors.Add(new MovieCollectionMovieRelationshipExtractor());

      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ImporterWorkerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
            Interlocked.Increment(ref _importerCount);
            break;
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            if (Interlocked.Decrement(ref _importerCount) == 0)
            {
              foreach (IMovieRelationshipExtractor extractor in _extractors)
                extractor.ClearCache();
            }
            break;
        }
      }
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    public RelationshipExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _extractors; }
    }
  }
}
