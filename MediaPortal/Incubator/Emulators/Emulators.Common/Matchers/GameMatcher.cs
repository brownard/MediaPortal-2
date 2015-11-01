using Emulators.Common.Games;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  public class GameMatcher
  {
    IOnlineMatcher _onlineMatcher = new TheGamesDbWrapper();

    public static GameMatcher Instance
    {
      get { return ServiceRegistration.Get<GameMatcher>(); }
    }

    public bool FindAndUpdateGame(GameInfo gameInfo)
    {
      TheGamesDbWrapper.TryGetTGDBId(gameInfo);
      NameProcessor.CleanupTitle(gameInfo);
      return _onlineMatcher.FindAndUpdateGame(gameInfo);
    }

    public bool TryGetImagePath(Guid matcherId, string onlineId, ImageType imageType, out string path)
    {
      return _onlineMatcher.TryGetImagePath(onlineId, imageType, out path);
    }
  }
}