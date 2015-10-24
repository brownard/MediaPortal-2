using Emulators.Common;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Server
{
    public class ServerPlugin : IPluginStateTracker
    {
      public void Activated(PluginRuntime pluginRuntime)
      {
        ServiceRegistration.Set<IMediaCategoryHelper>(new MediaCategoryHelper());
      }

      public bool RequestEnd()
      {
        return true;
      }

      public void Stop()
      {
        
      }

      public void Continue()
      {
        
      }

      public void Shutdown()
      {

      }
    }
}
