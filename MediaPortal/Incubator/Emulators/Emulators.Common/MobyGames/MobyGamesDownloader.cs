using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.MobyGames
{
  class MobyGamesDownloader : AbstractDownloader
  {
    protected override T Deserialize<T>(string response)
    {
      if (typeof(IMobyGamesResult).IsAssignableFrom(typeof(T)))
      {
        ConstructorInfo constructor = typeof(T).GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
          T item = (T)constructor.Invoke(null);
          if (((IMobyGamesResult)item).Deserialize(response))
            return item;
        }
      }
      return default(T);
    }
  }
}