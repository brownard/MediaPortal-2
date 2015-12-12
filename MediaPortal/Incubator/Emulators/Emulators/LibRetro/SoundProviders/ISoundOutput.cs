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
    bool Init(IntPtr windowHandler, int sampleRate);
    bool Play();
    void Pause();
    void UnPause();
    void SetVolume(int volume);
    void WriteSamples(short[] samples, int count, bool synchronise);
    int GetPlayedSize();
  }
}
