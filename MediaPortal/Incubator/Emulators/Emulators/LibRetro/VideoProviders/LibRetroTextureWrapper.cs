using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.VideoProviders
{
  public class LibRetroTextureWrapper : ITextureProvider
  {
    const int INT_COUNT_PER_PIXEL = 1;
    const int BYTE_COUNT_PER_PIXEL = 4;
    const int TEXTURE_BUFFER_LENGTH = 2;

    protected Texture[] _textures = new Texture[TEXTURE_BUFFER_LENGTH];
    protected int _currentTextureIndex;

    public Texture Texture
    {
      get { return _textures[_currentTextureIndex]; }
    }

    public void UpdateTexture(Device device, int[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      UpdateTexture(device, pixels, width, height, INT_COUNT_PER_PIXEL, bottomLeftOrigin);
    }

    public void UpdateTexture(Device device, byte[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      UpdateTexture(device, pixels, width, height, BYTE_COUNT_PER_PIXEL, bottomLeftOrigin);
    }

    protected void UpdateTexture<T>(Device device, T[] pixels, int width, int height, int countPerPixel, bool bottomLeftOrigin) where T : struct
    {
      if (pixels == null)
        return;

      Texture texture = GetOrCreateTexture(device, width, height);
      DataStream dataStream;
      DataRectangle rectangle = texture.LockRectangle(0, LockFlags.None, out dataStream);
      int padding = rectangle.Pitch - (width * sizeof(int));
      int countPerLine = width * countPerPixel;

      using (dataStream)
      {
        if (bottomLeftOrigin)
          WritePixelsBottomLeft(pixels, height, countPerLine, padding, dataStream);
        else
          WritePixels(pixels, height, countPerLine, padding, dataStream);
        texture.UnlockRectangle(0);
      }
    }

    protected Texture GetOrCreateTexture(Device device, int width, int height)
    {
      _currentTextureIndex = (_currentTextureIndex + 1) % _textures.Length;
      Texture texture = _textures[_currentTextureIndex];
      bool needsCreation = texture == null || texture.IsDisposed;
      if (!needsCreation)
      {
        SurfaceDescription surface = texture.GetLevelDescription(0);
        if (surface.Width != width || surface.Height != height)
        {
          texture.Dispose();
          needsCreation = true;
        }
      }
      if (needsCreation)
      {
        texture = new Texture(device, width, height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
        _textures[_currentTextureIndex] = texture;
      }
      return texture;
    }

    protected void WritePixels<T>(T[] pixels, int height, int countPerLine, int padding, DataStream dataStream) where T : struct
    {
      for (int i = 0; i < height; i++)
      {
        if (padding > 0 && i > 0)
          dataStream.Position += padding;
        dataStream.WriteRange(pixels, i * countPerLine, countPerLine);
      }
    }

    protected void WritePixelsBottomLeft<T>(T[] pixels, int height, int countPerLine, int padding, DataStream dataStream) where T : struct
    {
      for (int i = height - 1; i >= 0; i--)
      {
        if (padding > 0 && i < height - 1)
          dataStream.Position += padding;
        dataStream.WriteRange(pixels, i * countPerLine, countPerLine);
      }
    }

    public void Release()
    {
      for (int i = 0; i < _textures.Length; i++)
      {
        if (_textures[i] != null)
        {
          _textures[i].Dispose();
          _textures[i] = null;
        }
      }
    }

    public void Dispose()
    {
      Release();
    }
  }
}