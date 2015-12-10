﻿using SharpDX;
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
    protected Texture[] _textures = new Texture[2];
    protected int _currentTexture;

    public Texture Texture
    {
      get { return _textures[_currentTexture]; }
    }

    public void UpdateTexture(Device device, int[] pixels, int width, int height)
    {
      _currentTexture = (_currentTexture + 1) % _textures.Length;
      Texture texture = new Texture(device, width, height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
      DataStream dataStream;
      texture.LockRectangle(0, LockFlags.None, out dataStream);
      using (dataStream)
        dataStream.WriteRange(pixels, 0, width * height);
      texture.UnlockRectangle(0);
      Texture oldTexture = _textures[_currentTexture];
      if (oldTexture != null)
        oldTexture.Dispose();
      _textures[_currentTexture] = texture;
    }

    public void UpdateTexture(Device device, byte[] pixels, int width, int height, bool bottomLeftOrigin)
    {
      if (pixels == null)
        return;
      _currentTexture = (_currentTexture + 1) % _textures.Length;
      Texture texture = new Texture(device, width, height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
      DataStream dataStream;
      texture.LockRectangle(0, LockFlags.None, out dataStream);
      using (dataStream)
      {
        if (bottomLeftOrigin)
          FlipVertically(pixels, width, height, dataStream);
        else
          dataStream.WriteRange(pixels, 0, width * height * 4);
      }
      texture.UnlockRectangle(0);
      Texture oldTexture = _textures[_currentTexture];
      if (oldTexture != null)
        oldTexture.Dispose();
      _textures[_currentTexture] = texture;
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