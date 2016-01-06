using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class VideoInfo
  {
    public float DAR { get; set; }
    public int BufferWidth { get; set; }
    public int BufferHeight { get; set; }

    public int VirtualWidth
    {
      get
      {
        if (DAR <= 0)
          return BufferWidth;
        else if (DAR > 1.0f)
          return (int)(BufferHeight * DAR);
        else
          return BufferWidth;
      }
    }

    public int VirtualHeight
    {
      get
      {
        if (DAR <= 0)
          return BufferHeight;
        if (DAR < 1.0f)
          return (int)(BufferWidth / DAR);
        else
          return BufferHeight;
      }
    }
  }
}
