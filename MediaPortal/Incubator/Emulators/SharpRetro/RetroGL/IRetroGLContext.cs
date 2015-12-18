using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.RetroGL
{
  public interface IRetroGLContext : IDisposable
  {
    bool IsInit { get; }
    bool NeedsReset { get; set; }
    uint FrameBufferId { get; }
    bool BottomLeftOrigin { get; }
    void Init(int maxWidth, int maxHeight, bool depth, bool stencil, bool bottomLeftOrigin);
    byte[] GetPixels(int width, int height);
    IntPtr GetProcAddress(IntPtr sym);
  }
}
