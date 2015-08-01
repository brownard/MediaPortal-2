using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider
{
  /// <summary>
  /// 
  /// </summary>
  public interface IFanartImageSourceProvider
  {
    bool TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource);
  }
}
