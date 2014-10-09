#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.UiComponents.Media.Models
{
  public class LatestMediaModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string LATEST_MEDIA_MODEL_ID_STR = "11193401-D85D-4D50-9825-E9EB34D87062";

    // ID variables
    public static readonly Guid LATEST_MEDIA_MODEL_ID = new Guid(LATEST_MEDIA_MODEL_ID_STR);

    #endregion

    public const int QUERY_LIMIT = 5;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public ItemsList LatestMovies { get; private set; }

    public ItemsList LatestSeries { get; private set; }

    public ItemsList LatestImages { get; private set; }

    public ItemsList LatestAudio { get; private set; }

    private readonly ItemsList[] _knownLists;

    public LatestMediaModel()
    {
      LatestMovies = new ItemsList();
      LatestSeries = new ItemsList();
      LatestImages = new ItemsList();
      LatestAudio = new ItemsList();
      _knownLists = new[] { LatestMovies, LatestSeries, LatestImages, LatestAudio };
    }

    protected void ClearAll()
    {
      foreach (ItemsList list in _knownLists)
      {
        list.Clear();
        list.FireChange();
      }
    }

    protected void Update()
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
      {
        ClearAll();
        return;
      }

      FillList(contentDirectory, Consts.NECESSARY_MOVIES_MIAS, LatestMovies, item => new MovieItem(item));
      FillList(contentDirectory, Consts.NECESSARY_SERIES_MIAS, LatestSeries, item => new SeriesItem(item));
      FillList(contentDirectory, Consts.NECESSARY_IMAGE_MIAS, LatestImages, item => new ImageItem(item));
      FillList(contentDirectory, Consts.NECESSARY_AUDIO_MIAS, LatestAudio, item => new AudioItem(item));
    }

    protected static void FillList(IContentDirectory contentDirectory, Guid[] necessaryMIAs, ItemsList list, MediaItemToListItemAction converterAction)
    {
      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, null)
      {
        Limit = QUERY_LIMIT, // Last 5 imported items
        SortInformation = new List<SortInformation> { new SortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
      };

      var items = contentDirectory.Search(query, false);
      list.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        list.Add(listItem);
      }
      list.FireChange();
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return LATEST_MEDIA_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Update();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't disable the current navigation data when we leave our model - the last navigation data must be
      // available in sub workflows, for example to make the GetMediaItemsFromCurrentView method work
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // The last navigation data was not disabled so we don't need to enable it here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
