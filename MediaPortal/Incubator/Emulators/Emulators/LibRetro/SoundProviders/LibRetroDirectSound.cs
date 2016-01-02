using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Settings;
using SharpDX.DirectSound;
using SharpDX.Multimedia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.LibRetro.SoundProviders
{
  public class LibRetroDirectSound : ISoundOutput
  {
    protected DirectSound _directSound;
    protected SecondarySoundBuffer _secondaryBuffer;
    protected int _sampleRate;
    protected int _bufferBytes;
    protected int _nextWrite;
    protected double _samplesPerMs;

    public int SampleRate
    {
      get { return _sampleRate; }
    }

    public bool Init(IntPtr windowHandler, int sampleRate)
    {
      _sampleRate = sampleRate;
      try
      {
        InitializeDirectSound(windowHandler);
        InitializeAudio();
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("LibRetroDirectSound: Failed to initialise device", ex);
        return false;
      }
    }

    void InitializeDirectSound(IntPtr windowHandler)
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      if (settings == null || settings.AudioRenderer == null || string.IsNullOrEmpty(settings.AudioRenderer.CLSID))
        _directSound = new DirectSound();
      else
        _directSound = new DirectSound(new Guid(settings.AudioRenderer.CLSID));
      // Set the cooperative level to priority so the format of the primary sound buffer can be modified.
      _directSound.SetCooperativeLevel(windowHandler, CooperativeLevel.Priority);
    }

    protected void InitializeAudio()
    {
      var format = new WaveFormat(_sampleRate, 16, 2);
      var buffer = new SoundBufferDescription();
      buffer.Flags = BufferFlags.GlobalFocus | BufferFlags.ControlVolume;
      buffer.BufferBytes = format.AverageBytesPerSecond / 8;
      buffer.Format = format;
      buffer.AlgorithmFor3D = Guid.Empty;
      // Create a temporary sound buffer with the specific buffer settings.
      _secondaryBuffer = new SecondarySoundBuffer(_directSound, buffer);
      _bufferBytes = _secondaryBuffer.Capabilities.BufferBytes;
      _samplesPerMs = format.AverageBytesPerSecond / (sizeof(short) * 1000d);
    }

    public int GetPlayedSize()
    {
      int pPos;
      int wPos;
      _secondaryBuffer.GetCurrentPosition(out pPos, out wPos);
      return wPos < _nextWrite ? wPos + _bufferBytes - _nextWrite : wPos - _nextWrite;
    }
    
    public void WriteSamples(short[] samples, int count, bool synchronise)
    {
      if (count == 0)
        return;
      if (synchronise)
        Synchronize(count);
      int bytes = GetPlayedSize();
      if (bytes < 1)
        return;
      if (_secondaryBuffer.Status == (int)BufferStatus.BufferLost)
        _secondaryBuffer.Restore();
      count = Math.Min(count, bytes / 2);
      _secondaryBuffer.Write(samples, 0, count, _nextWrite, LockFlags.None);
      _nextWrite += count * 2;
      if (_nextWrite >= _bufferBytes)
        _nextWrite -= _bufferBytes;
    }

    public bool Play()
    {
      try
      {
        // Set the position at the beginning of the sound buffer.
        _secondaryBuffer.CurrentPosition = 0;
        // Set volume of the buffer to 100%
        _secondaryBuffer.Volume = 0;
        // Play the contents of the secondary sound buffer.
        _secondaryBuffer.Play(0, PlayFlags.Looping);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("LibRetroDirectSound: Failed to start playback", ex);
        return false;
      }
      return true;
    }

    public void Pause()
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Stop();
    }

    public void UnPause()
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Play(0, PlayFlags.Looping);
    }

    public void SetVolume(int volume)
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Volume = volume;
    }

    protected void Synchronize(int count)
    {
      int samplesNeeded = GetSamplesNeeded();
      //ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("************Audio count {0} samplesneeded {1} total size {2}", count, samplesNeeded, _bufferBytes);
      while (samplesNeeded < count)
      {
        int sleepTime = (int)((count - samplesNeeded) / _samplesPerMs);
        Thread.Sleep(Math.Max(sleepTime / 2, 1));
        samplesNeeded = GetSamplesNeeded();
      }
    }

    protected int GetSamplesNeeded()
    {
      return GetPlayedSize() / sizeof(short);
    }

    public void Dispose()
    {
      if (_secondaryBuffer != null)
      {
        _secondaryBuffer.Dispose();
        _secondaryBuffer = null;
      }
      if (_directSound != null)
      {
        _directSound.Dispose();
        _directSound = null;
      }
    }
  }
}
