using Emulators.Common.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  interface IOnlineMatcher
  {
    Guid MatcherId { get; }
    bool TryGetBestMatch(GameInfo gameInfo);
    bool TryGetImagePath(string id, ImageType imageType, out string path);
  }
}
