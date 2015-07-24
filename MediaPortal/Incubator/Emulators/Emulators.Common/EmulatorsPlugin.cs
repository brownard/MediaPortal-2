using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class EmulatorsPlugin : IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(GameAspect.Metadata);
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
