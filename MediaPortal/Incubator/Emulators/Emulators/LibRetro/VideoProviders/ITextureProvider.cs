﻿using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.VideoProviders
{
  public interface ITextureProvider : IDisposable
  {
    Texture Texture { get; }
    void UpdateTexture(Device device, int[] pixels, int width, int height);
    void Release();
  }
}
