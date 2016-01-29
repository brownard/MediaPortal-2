using SharpGL;
using SharpGL.Version;
using SharpRetro.RetroGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpRetro.LibRetro;

namespace Emulators.LibRetro.GLContexts
{
  public class RetroGLContextProvider : FBORenderContextProvider, IRetroGLContext
  {
    [DllImport("opengl32", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(IntPtr function_name);

    protected LibRetroCore.retro_hw_context_reset_t _contextReset;
    protected LibRetroCore.retro_hw_context_reset_t _contextDestroy;
    protected byte[] _pixels;
    protected bool _isCreated;
    protected bool _bottomLeftOrigin;
    protected int _maxWidth;
    protected int _maxHeight;

    public byte[] Pixels
    {
      get { return _pixels; }
    }

    public bool BottomLeftOrigin
    {
      get { return _bottomLeftOrigin; }
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
      _maxWidth = width;
      _maxHeight = height;
      _pixels = new byte[_maxWidth * _maxHeight * 4];
      Create(OpenGLVersion.OpenGL2_1, new OpenGL(), _maxWidth, _maxHeight, 32, null);
      _isCreated = true;
      if (_contextReset != null)
        _contextReset();
    }

    public IntPtr GetProcAddress(IntPtr sym)
    {
      IntPtr ptr = wglGetProcAddress(sym);
      return ptr;
    }

    public uint GetCurrentFramebuffer()
    {
      return frameBufferID;
    }

    public void OnFrameBufferReady(int width, int height)
    {
      UpdatePixels(width, height);
    }

    protected void UpdatePixels(int width, int height)
    {
      if (deviceContextHandle != IntPtr.Zero)
      {
        //  Set the read buffer.
        gl.ReadBuffer(OpenGL.GL_COLOR_ATTACHMENT0_EXT);
        gl.ReadPixels(0, 0, width, height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, _pixels);
      }
    }

    public void Dispose()
    {
      if (_isCreated)
        Destroy();
    }
  }
}