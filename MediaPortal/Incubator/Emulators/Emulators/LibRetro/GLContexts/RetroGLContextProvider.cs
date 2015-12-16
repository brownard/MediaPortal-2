using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.Version;
using SharpRetro.RetroGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.GLContexts
{
  public class RetroGLContextProvider : FBORenderContextProvider, IRetroGLContext
  {
    [DllImport("opengl32", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(IntPtr function_name);

    protected byte[] _pixels;
    protected bool _isInit;
    protected bool _needsReset;
    protected bool _bottomLeftOrigin;

    public bool IsInit
    {
      get { return _isInit; }
    }

    public bool NeedsReset
    {
      get { return _needsReset; }
      set { _needsReset = value; }
    }

    public uint FrameBufferId
    {
      get { return frameBufferID; }
    }

    public byte[] Pixels
    {
      get { return _pixels; }
    }

    public bool BottomLeftOrigin
    {
      get { return _bottomLeftOrigin; }
    }

    public void Init(int maxWidth, int maxHeight, bool depth, bool stencil, bool bottomLeftOrigin)
    {
      if (_isInit)
        return;
      Create(OpenGLVersion.OpenGL2_1, new OpenGL(), maxWidth, maxHeight, 32, null);
      _isInit = true;
      _needsReset = true;
      _bottomLeftOrigin = bottomLeftOrigin;
    }

    public IntPtr GetProcAddress(IntPtr sym)
    {
      IntPtr ptr = wglGetProcAddress(sym);
      //if (ptr == IntPtr.Zero)
      //  ServiceRegistration.Get<ILogger>().Warn("GLContextProvider: Unable to get ProcAddress for symbol '{0}'", Marshal.PtrToStringAnsi(sym));
      return ptr;
    }

    public void FrameBufferReady(int width, int height)
    {
      _pixels = GetPixels(width, height);
    }

    public byte[] GetPixels(int width, int height)
    {
      if (deviceContextHandle != IntPtr.Zero)
      {
        //  Set the read buffer.
        gl.ReadBuffer(OpenGL.GL_COLOR_ATTACHMENT0_EXT);
        byte[] pixels = new byte[width * height * 4];
        gl.ReadPixels(0, 0, width, height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, pixels);
        return pixels;
      }
      return null;
    }

    public void Dispose()
    {
      MakeCurrent();
      Destroy();
    }
  }
}