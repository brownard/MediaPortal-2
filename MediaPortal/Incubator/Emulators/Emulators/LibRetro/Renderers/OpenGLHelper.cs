using SharpGL;
using SharpRetro.LibRetro;
using SharpRetro.OpenGL;
using SharpRetro.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Renderers
{
  public class OpenGLHelper : IGLContext, IDisposable
  {
    [DllImport("opengl32", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(IntPtr function_name);

    OpenGL _gl;
    GlContextProvider _context;
    byte[] _pixels;

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
      get { return _context.FrameBufferId; }
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
      _gl = new OpenGL();
      _context = new GlContextProvider();
      var result = _context.Create(SharpGL.Version.OpenGLVersion.OpenGL2_1, _gl, maxWidth, maxHeight, 32, null);
      _isInit = true;
      _needsReset = true;
      _bottomLeftOrigin = bottomLeftOrigin;
    }

    public IntPtr GetProcAddress(IntPtr sym)
    {
      string dbg = Marshal.PtrToStringAnsi(sym);
      var ptr = wglGetProcAddress(sym);
      return ptr;
    }

    public void FrameBufferReady(int width, int height)
    {
      _pixels = _context.GetPixels(width, height);
    }

    public void Dispose()
    {
      if (_context != null)
      {
        _context.MakeCurrent();
        _context.Destroy();
        _context = null;
      }
    }
  }
}