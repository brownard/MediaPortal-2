using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.SoundProviders
{
  public interface ISoundOutput : IDisposable
  {
    int SampleRate { get; }
    bool Play();
    void WriteSamples(short[] samples, int count, bool synchronise);
    int GetPlayedSize();
  }
}
