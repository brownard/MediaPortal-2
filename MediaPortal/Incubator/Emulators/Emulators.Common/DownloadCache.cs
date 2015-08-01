using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  class DownloadCache
  {
    ConcurrentDictionary<string, byte[]> _cache = new ConcurrentDictionary<string, byte[]>();

    public void Add(string url, byte[] response)
    {
      _cache.TryAdd(url, response);
    }

    public bool TryGetFromCache(string url, out byte[] response)
    {
      return _cache.TryGetValue(url, out response);
    }
  }
}
