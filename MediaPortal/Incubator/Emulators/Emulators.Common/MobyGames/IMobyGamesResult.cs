using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.MobyGames
{
  interface IMobyGamesResult
  {
    bool Deserialize(string response);
  }
}
