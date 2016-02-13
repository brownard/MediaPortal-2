using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models.Navigation
{
  public class LibRetroCoreItem : ListItem
  {
    protected AbstractProperty _downloadedProperty = new WProperty(typeof(bool), false);
    
    public AbstractProperty DownloadedProperty
    {
      get { return _downloadedProperty; }
    }

    public bool Downloaded
    {
      get { return (bool)_downloadedProperty.GetValue(); }
      set { _downloadedProperty.SetValue(value); }
    }
  }
}
