using Emulators.Common.Games;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  public class GameMatcher
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    const int MAX_SEARCH_DISTANCE = 2;
    TheGamesDbWrapper _theGamesDb = new TheGamesDbWrapper();

    public static GameMatcher Instance
    {
      get { return ServiceRegistration.Get<GameMatcher>(); }
    }

    public bool FindAndUpdateGame(GameInfo gameInfo)
    {
      GameResult result;
      if (!FindUniqueGame(gameInfo, out result))
        return false;

      Game game = result.Game;
      gameInfo.GameName = game.GameTitle;
      gameInfo.GamesDbId = game.Id;
      gameInfo.Certification = game.ESRB;
      gameInfo.Description = game.Overview;
      gameInfo.Developer = game.Developer;
      if (game.Genres != null && game.Genres.Genres != null)
        gameInfo.Genres.AddRange(game.Genres.Genres);
      gameInfo.Rating = game.Rating;
      DateTime releaseDate;
      if (DateTime.TryParse(game.ReleaseDate, TheGamesDbWrapper.DATE_CULTURE, DateTimeStyles.None, out releaseDate))
        gameInfo.ReleaseDate = releaseDate;
      if (game.Id > 0)
        _theGamesDb.ScheduleDownload(game.Id);
      return true;
    }

    protected bool FindUniqueGame(GameInfo gameInfo, out GameResult result)
    {
      result = null;
      NameProcessor.CleanupTitle(gameInfo);
      if (_theGamesDb.TryGetFromStorage(gameInfo.GameName, gameInfo.Platform, out result))
      {
        Logger.Debug("GameMatcher: Retrieved from cache: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return true;
      }

      List<GameSearchResult> results;
      if (!_theGamesDb.Search(gameInfo.GameName, gameInfo.Platform, out results))
      {
        Logger.Debug("GameMatcher: No results found for game: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return false;
      }

      Logger.Debug("GameMatcher: Found {0} items for game: '{1}' - '{2}'", results.Count, gameInfo.GameName, gameInfo.Platform);
      results = results.FindAll(r => r.GameTitle == gameInfo.GameName || NameProcessor.GetLevenshteinDistance(r.GameTitle, gameInfo.GameName) <= MAX_SEARCH_DISTANCE);
      if (results.Count > 0 && _theGamesDb.Get(results[0].Id, out result))
      {
        Logger.Debug("GameMatcher: Matched '{0}' to game: '{1}' - '{2}'", results[0].GameTitle, gameInfo.GameName, gameInfo.Platform);
        _theGamesDb.AddToStorage(gameInfo.GameName, gameInfo.Platform, result.Game.Id);
        return true;
      }
      return false;
    }
  }
}