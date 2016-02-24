using SharpGL.Version;
using SharpRetro.RetroGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;
using SharpDX.Direct3D9;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace Emulators.LibRetro.GLContexts
{
  public class RetroGLContextProvider : IRetroGLContext
  {
    [DllImport("opengl32", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(IntPtr function_name);

    protected LibRetroCore.retro_hw_get_current_framebuffer_t _getCurrentFramebufferDlgt;
    protected LibRetroCore.retro_hw_get_proc_address_t _getProcAddressDlgt;
    protected LibRetroCore.retro_hw_context_reset_t _contextReset;
    protected LibRetroCore.retro_hw_context_reset_t _contextDestroy;

    protected FBORenderContextProvider _fboContextProvider;
    protected DXRenderContextProvider _dxContextProvider;

    protected bool _hasDXContext;
    protected byte[] _pixels;
    protected bool _isCreated;
    protected bool _bottomLeftOrigin;
    protected bool _isTextureDirty;
    protected int _currentWidth;
    protected int _currentHeight;

    public bool BottomLeftOrigin
    {
      get { return _bottomLeftOrigin; }
    }

    public Texture Texture
    {
      get { return _dxContextProvider != null ? _dxContextProvider.CurrentTexture : null; }
    }

    public bool IsTextureDirty
    {
      get { return _isTextureDirty; }
      set { _isTextureDirty = value; }
    }

    public int CurrentWidth
    {
      get { return _currentWidth; }
    }

    public int CurrentHeight
    {
      get { return _currentHeight; }
    }

    public bool HasDXContext
    {
      get { return _hasDXContext; }
    }

    public LibRetroCore.retro_hw_get_current_framebuffer_t GetCurrentFramebufferDlgt
    {
      get { return _getCurrentFramebufferDlgt; }
    }

    public LibRetroCore.retro_hw_get_proc_address_t GetProcAddressDlgt
    {
      get { return _getProcAddressDlgt; }
    }

    public RetroGLContextProvider(bool dxInteropContext)
    {
      _hasDXContext = dxInteropContext;
      _getCurrentFramebufferDlgt = new LibRetroCore.retro_hw_get_current_framebuffer_t(GetCurrentFramebuffer);
      _getProcAddressDlgt = new LibRetroCore.retro_hw_get_proc_address_t(GetProcAddress);
    }

    public void Init(bool depth, bool stencil, bool bottomLeftOrigin, LibRetroCore.retro_hw_context_reset_t contextReset, LibRetroCore.retro_hw_context_reset_t contextDestroy)
    {
      _bottomLeftOrigin = bottomLeftOrigin;
      _contextReset = contextReset;
      _contextDestroy = contextDestroy;
    }

    public void Create(int width, int height)
    {
      if (_isCreated)
        return;
      
      if (_hasDXContext)
      {
        _dxContextProvider = new DXRenderContextProvider();
        _fboContextProvider = _dxContextProvider;
        _dxContextProvider.Create(OpenGLVersion.OpenGL2_1, new OpenGLEx(), width, height, 32, null);
        if (!_dxContextProvider.HasDXContext)
        {
          _dxContextProvider = null;
          _hasDXContext = false;
          ServiceRegistration.Get<ILogger>().Warn("RetroGLContextProvider: WGL_NV_DX_interop extensions are not supported by the graphics driver, falling back to read back. This will reduce performance");
        }
      }
      else
      {
        _fboContextProvider = new FBORenderContextProvider();
        _fboContextProvider.Create(OpenGLVersion.OpenGL2_1, new OpenGLEx(), width, height, 32, null);
      }

      if (!_hasDXContext)
        _pixels = new byte[width * height * 4];

      _isCreated = true;
      if (_contextReset != null)
        _contextReset();
    }

    public byte[] ReadPixels(int width, int height)
    {
      if (!_isCreated || _hasDXContext)
        return null;
      _fboContextProvider.ReadPixels(_pixels, width, height);
      return _pixels;
    }

    public void UpdateCurrentTexture(int width, int height)
    {
      if (!_isCreated || !_hasDXContext)
        return;
      _dxContextProvider.UpdateCurrentTexture(_bottomLeftOrigin);
      _currentWidth = width;
      _currentHeight = height;
      _isTextureDirty = true;
    }

    protected IntPtr GetProcAddress(IntPtr sym)
    {
      IntPtr ptr = wglGetProcAddress(sym);
      return ptr;
    }

    protected uint GetCurrentFramebuffer()
    {
      return _fboContextProvider.FramebufferId;
    }

    public void Dispose()
    {
      if (_isCreated)
        _fboContextProvider.Destroy();
    }
  }
}