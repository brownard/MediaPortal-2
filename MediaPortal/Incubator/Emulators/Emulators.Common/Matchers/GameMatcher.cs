using Emulators.Common.Games;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using System;
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

    public Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      TheGamesDbWrapper.TryGetTGDBId(gameInfo);
      NameProcessor.CleanupTitle(gameInfo);
      return _onlineMatcher.FindAndUpdateGameAsync(gameInfo);
    }

    public Task DownloadFanArtAsync(string onlineId)
    {
      return _onlineMatcher.DownloadFanArtAsync(onlineId);
    }

    public bool TryGetImagePath(Guid matcherId, string onlineId, ImageType imageType, out string path)
    {
      return _onlineMatcher.TryGetImagePath(onlineId, imageType, out path);
    }
  }
}
