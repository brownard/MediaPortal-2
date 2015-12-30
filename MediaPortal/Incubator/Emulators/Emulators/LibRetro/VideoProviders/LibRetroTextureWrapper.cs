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
    const int TEXTURE_BUFFER_LENGTH = 2;
    protected Texture[] _textures = new Texture[TEXTURE_BUFFER_LENGTH];
    protected int _currentTextureIndex;

    public Texture Texture
    {
      get { return _textures[_currentTextureIndex]; }
    }

    public void UpdateTexture(Device device, int[] pixels, int width, int height)
    {
      if (pixels == null)
        return;
      _currentTextureIndex = (_currentTextureIndex + 1) % _textures.Length;
      Texture texture = GetOrCreateTexture(device, width, height);
      DataStream dataStream;
      texture.LockRectangle(0, LockFlags.None, out dataStream);
      using (dataStream)
      {
        dataStream.WriteRange(pixels, 0, width * height);
        texture.UnlockRectangle(0);
      }
    }

    public void UpdateTexture(Device device, byte[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      if (pixels == null)
        return;
      _currentTextureIndex = (_currentTextureIndex + 1) % _textures.Length;
      Texture texture = GetOrCreateTexture(device, width, height);
      DataStream dataStream;
      texture.LockRectangle(0, LockFlags.None, out dataStream);
      using (dataStream)
      {
        if (bottomLeftOrigin)
          FlipVertically(pixels, width, height, dataStream);
        else
          dataStream.WriteRange(pixels, 0, width * height * 4);
        texture.UnlockRectangle(0);
      }
    }

    protected void FlipVertically(byte[] pixels, int width, int height, DataStream dataStream)
    {
      int byteWidth = width * 4;
      int currentRow = height - 1;
      while (currentRow >= 0)
      {
        int offset = currentRow * byteWidth;
        dataStream.WriteRange(pixels, offset, byteWidth);
        currentRow--;
      }
    }

    protected Texture GetOrCreateTexture(Device device, int width, int height)
    {
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