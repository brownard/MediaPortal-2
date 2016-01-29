using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.RetroGL
{
  public interface IRetroGLContext : IDisposable
  {
    bool BottomLeftOrigin { get; }
    byte[] Pixels { get; }
    void Init(bool depth, bool stencil, bool bottomLeftOrigin, LibRetroCore.retro_hw_context_reset_t contextReset, LibRetroCore.retro_hw_context_reset_t contextDestroy);
    void Create(int width, int height);
    IntPtr GetProcAddress(IntPtr symbol);
    uint GetCurrentFramebuffer();
    void OnFrameBufferReady(int width, int height);
  }
}
