using SharpGL;
using SharpGL.RenderContextProviders;
using SharpGL.Version;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Renderers
{
  class GlContextProvider : FBORenderContextProvider
  {
    public uint FrameBufferId
    {
      get { return frameBufferID; }
    }

    /// <summary>
    /// Creates the render context provider. Must also create the OpenGL extensions.
    /// </summary>
    /// <param name="openGLVersion">The desired OpenGL version.</param>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="bitDepth">The bit depth.</param>
    /// <param name="parameter">The parameter</param>
    /// <returns></returns>
    public bool Create(OpenGLVersion openGLVersion, OpenGL gl, int width, int height, int bitDepth, bool depthBuffer, bool stencilBuffer, object parameter)
    {
      this.gl = gl;

      //  Call the base class. 	        
      base.Create(openGLVersion, gl, width, height, bitDepth, parameter);

      uint[] ids = new uint[1];

      //  First, create the frame buffer and bind it.
      ids = new uint[1];
      gl.GenFramebuffersEXT(1, ids);
      frameBufferID = ids[0];
      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, frameBufferID);

      //	Create the colour render buffer and bind it, then allocate storage for it.
      gl.GenRenderbuffersEXT(1, ids);
      colourRenderBufferID = ids[0];
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
      gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, width, height);

      //	Create the depth render buffer and bind it, then allocate storage for it.
      gl.GenRenderbuffersEXT(1, ids);
      depthRenderBufferID = ids[0];
      gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, depthRenderBufferID);
      gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_DEPTH_COMPONENT24, width, height);

      //  Set the render buffer for colour and depth.
      gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT,
          OpenGL.GL_RENDERBUFFER_EXT, colourRenderBufferID);
      gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_DEPTH_ATTACHMENT_EXT,
          OpenGL.GL_RENDERBUFFER_EXT, depthRenderBufferID);

      dibSectionDeviceContext = Win32.CreateCompatibleDC(deviceContextHandle);

      //  Create the DIB section.
      dibSection.Create(dibSectionDeviceContext, width, height, bitDepth);

      return true;
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
  }
}