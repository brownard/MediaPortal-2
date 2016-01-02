using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroSaveStateHandler
  {
    protected const string SAVE_RAM_EXTENSION = ".srm";
    protected int _autoSaveInterval;
    protected LibRetroEmulator _retroEmulator;
    protected string _gamePath;
    protected string _saveDirectory;
    protected DateTime _lastSaveTime = DateTime.MinValue;
    protected byte[] _lastSaveRam;

    public LibRetroSaveStateHandler(LibRetroEmulator retroEmulator, string gamePath, string saveDirectory, int autoSaveIntervalMs)
    {
      _retroEmulator = retroEmulator;
      _gamePath = gamePath;
      _saveDirectory = saveDirectory;
      _autoSaveInterval = autoSaveIntervalMs;
    }

    public void LoadSaveRam()
    {
      string saveFile = GetSaveFile(SAVE_RAM_EXTENSION);
      byte[] saveRam;
      if (TryReadFromFile(saveFile, out saveRam))
      {
        _retroEmulator.LoadState(LibRetroCore.RETRO_MEMORY.SAVE_RAM, saveRam);
        _lastSaveRam = saveRam;
      }
    }

    public void SaveSaveRam()
    {
      byte[] saveRam = _retroEmulator.SaveState(LibRetroCore.RETRO_MEMORY.SAVE_RAM);
      if (saveRam == null)
        return;
      TryWriteToFile(GetSaveFile(SAVE_RAM_EXTENSION), saveRam);
    }

    public void AutoSave()
    {
      DateTime now = DateTime.Now;
      if ((now - _lastSaveTime).TotalMilliseconds < _autoSaveInterval)
        return;
      _lastSaveTime = now;
      byte[] saveRam = _retroEmulator.SaveState(LibRetroCore.RETRO_MEMORY.SAVE_RAM);
      if (!ShouldSave(_lastSaveRam, saveRam))
        return;
      string savePath = GetSaveFile(SAVE_RAM_EXTENSION);
      if (TryWriteToFile(savePath, saveRam))
      {
        ServiceRegistration.Get<ILogger>().Debug("LibRetroSaveStateHandler: Auto saved to '{0}'", GetSaveFile(SAVE_RAM_EXTENSION));
        _lastSaveRam = saveRam;
      }
    }

    protected bool TryReadFromFile(string path, out byte[] fileBytes)
    {
      try
      {
        if (File.Exists(path))
        {
          fileBytes = File.ReadAllBytes(path);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroSaveStateHandler: Error reading from path '{0}':", ex, path);
      }
      fileBytes = null;
      return false;
    }

    protected bool TryWriteToFile(string path, byte[] fileBytes)
    {
      try
      {
        DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(path));
        if (!directory.Exists)
          directory.Create();
        File.WriteAllBytes(path, fileBytes);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroSaveStateHandler: Error writing to path '{0}':", ex, path);
      }
      return false;
    }

    protected string GetSaveFile(string extension)
    {
      return Path.Combine(_saveDirectory, Path.GetFileNameWithoutExtension(_gamePath) + extension);
    }

    protected static bool ShouldSave(byte[] original, byte[] updated)
    {
      if (updated == null || updated.Length == 0)
        return false;
      if (original == null || original.Length == 0)
        return true;
      if (original.Length != updated.Length)
        return true;
      for (int i = 0; i < original.Length; i++)
        if (original[i] != updated[i])
          return true;
      return false;
    }
  }
}
