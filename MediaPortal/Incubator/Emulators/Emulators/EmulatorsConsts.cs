﻿using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators
{
  static class EmulatorsConsts
  {
    public const string MEDIA_NAVIGATION_MODE = "Games";

    public static Guid WF_MEDIA_NAVIGATION_ROOT_STATE = new Guid("DFC41902-DE22-4AEF-B9D2-C369BD79C4E0");
    public static Guid[] NECESSARY_GAME_MIAS = new[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          new Guid("71D500E8-F2C3-4DAF-8CE6-A89DFE8FD96E") /* GameAspect.ASPECT_ID*/
      };

    public const string RES_GAMES_VIEW_NAME = "[Emulators.GamesRootViewName]";

    public const string SCREEN_GAMES_SHOW_ITEMS = "GameShowItems";
    public const string RES_SHOW_ALL_GAME_ITEMS_MENU_ITEM = "[Emulators.ShowAllGameItemsMenuItem]";
    public const string RES_FILTER_GAME_ITEMS_NAVBAR_DISPLAY_LABEL = "[Emulators.FilterGameItemsNavbarDisplayLabel]";
  }
}