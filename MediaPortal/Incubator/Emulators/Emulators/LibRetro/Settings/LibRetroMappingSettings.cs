using Emulators.LibRetro.Controllers.Mapping;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Settings
{
  public class LibRetroMappingSettings
  {
    protected List<RetroPadMapping> _mappings;

    [Setting(SettingScope.User, null)]
    public List<RetroPadMapping> Mappings
    {
      get
      {
        if (_mappings == null)
          _mappings = new List<RetroPadMapping>();
        return _mappings;
      }
      set { _mappings = value; }
    }
  }
}
