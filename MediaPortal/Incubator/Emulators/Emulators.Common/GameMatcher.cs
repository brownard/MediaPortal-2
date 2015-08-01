using Emulators.Common.TheGamesDb;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class GameMatcher
  {
    TheGamesDbWrapper _theGamesDb = new TheGamesDbWrapper();

    public bool FindAndUpdateGame(GameInfo game)
    {
      GameResult match;
      var results = _theGamesDb.Search(game.GameName, game.Platform);
      if (results.Length > 0 && (match = _theGamesDb.Get(results[0].Id)) != null)
      {
        game.GameName = match.Game.GameTitle;
        game.GamesDbId = match.Game.Id;
        game.Certification = match.Game.ESRB;
        game.Description = match.Game.Overview;
        game.Developer = match.Game.Developer;
        game.Genres.AddRange(match.Game.Genres.Genres);
        game.Platform = match.Game.Platform;
        game.Rating = match.Game.Rating;
        DateTime releaseDate;
        if (DateTime.TryParse(match.Game.ReleaseDate, out releaseDate))
          game.Year = releaseDate.Year;
        return true;
      }
      return false;
    }

    public void DownloadCovers(int id)
    {
      _theGamesDb.DownloadCovers(id);
    }

    public void DownloadFanart(int id)
    {
      _theGamesDb.DownloadFanart(id);
    }
  }
}
