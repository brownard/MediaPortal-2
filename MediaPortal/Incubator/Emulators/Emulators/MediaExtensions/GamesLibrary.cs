using Emulators.Models.Navigation;
using MediaPortal.Common.Commands;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions
{
  public class GamesLibrary : BaseNavigationInitializer
  {
    public static void RegisterOnMediaLibrary()
    {
      MediaNavigationModel.RegisterMediaNavigationInitializer(new GamesLibrary());
      // All non-default media item aspects must be registered
      //IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      //miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public GamesLibrary()
    {
      _mediaNavigationMode = EmulatorsConsts.MEDIA_NAVIGATION_MODE;
      _mediaNavigationRootState = EmulatorsConsts.WF_MEDIA_NAVIGATION_ROOT_STATE;
      _viewName = EmulatorsConsts.RES_GAMES_VIEW_NAME;
      _necessaryMias = EmulatorsConsts.NECESSARY_GAME_MIAS;
    }

    protected override void Prepare()
    {
      base.Prepare();

      AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new GameItem(mi)
      {
        Command = new MethodDelegateCommand(() =>
          {
            GameLauncher.LaunchGame(new PJ64Config(), mi);
          })
      };

      _defaultScreen = new GamesShowItemsScreenData(picd);
      _availableScreens = new List<AbstractScreenData>
        {
          _defaultScreen,
          //new RecordingFilterByNameScreenData(),
          //new RecordingsFilterByChannelScreenData(),
          //new VideosFilterByActorScreenData(),
          //new VideosFilterByDirectorScreenData(),
          //new VideosFilterByWriterScreenData(),
          //new VideosFilterByGenreScreenData(),
          //new VideosFilterByYearScreenData(),
          //new VideosFilterBySystemScreenData(),
          //new VideosSimpleSearchScreenData(picd),
        };

      //_defaultSorting = new SortByRecordingDateDesc();
      _availableSortings = new List<Sorting>
        {
          //_defaultSorting,
          new SortByTitle(),
          //new VideoSortByFirstGenre(),
          //new VideoSortByDuration(),
          //new VideoSortByFirstActor(),
          //new VideoSortByFirstDirector(),
          //new VideoSortByFirstWriter(),
          //new VideoSortBySize(),
          //new VideoSortByAspectRatio(),
          //new SortBySystem(),
        };

      var optionalMias = new Guid[]
      {
        //MovieAspect.ASPECT_ID,
        //SeriesAspect.ASPECT_ID,
        //AudioAspect.ASPECT_ID,
        //VideoAspect.ASPECT_ID,
        //ImageAspect.ASPECT_ID
      }.Union(MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode));

      //_customRootViewSpecification = new StackingViewSpecification(_viewName, null, _necessaryMias, optionalMias, true)
      //{
      //  MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
      //};

      _customRootViewSpecification = new MediaLibraryQueryViewSpecification(_viewName, null, _necessaryMias, optionalMias, true);
    }
  }
}
