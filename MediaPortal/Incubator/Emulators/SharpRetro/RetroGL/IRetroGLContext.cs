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
    LibRetroCore.retro_hw_get_current_framebuffer_t GetCurrentFramebufferDlgt { get; }
    LibRetroCore.retro_hw_get_proc_address_t GetProcAddressDlgt { get; }
    void Init(bool depth, bool stencil, bool bottomLeftOrigin, LibRetroCore.retro_hw_context_reset_t contextReset, LibRetroCore.retro_hw_context_reset_t contextDestroy);
    void Create(int width, int height);
  }
}
