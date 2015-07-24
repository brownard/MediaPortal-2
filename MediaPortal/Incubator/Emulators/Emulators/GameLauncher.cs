using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators
{
  public class GameLauncher
  {
    const string WILDCARD_GAME_PATH = "<gamepath>";

    protected Process _process;
    protected EmulatorConfiguration _emulatorConfiguration;
    protected string _gamePath;

    public static void LaunchGame(EmulatorConfiguration emulatorConfiguration, MediaItem mediaItem)
    {
      var locator = mediaItem.GetResourceLocator();
      var accessor = locator.CreateAccessor();
      var launcher = new GameLauncher(emulatorConfiguration, accessor.ResourcePathName);
      launcher.Init();
      launcher.Launch();
    }

    public GameLauncher(EmulatorConfiguration emulatorConfiguration, string gamePath)
    {
      _emulatorConfiguration = emulatorConfiguration;
      _gamePath = gamePath;
    }

    public event EventHandler GameExited;
    protected virtual void OnGameExited(EventArgs e)
    {
      if (GameExited != null)
        GameExited(this, e);
    }

    public void Init()
    {
      initProcess();
    }

    public bool Launch()
    {
      return tryStartProcess();
    }

    void initProcess()
    {
      string arguments = CreateArguments(_emulatorConfiguration.Arguments, _gamePath);
      _process = new Process();
      _process.StartInfo = new ProcessStartInfo(_emulatorConfiguration.Path, arguments);
      _process.StartInfo.WorkingDirectory = _emulatorConfiguration.WorkingDirectory;
      _process.EnableRaisingEvents = true;
      _process.Exited += process_Exited;
    }

    bool tryStartProcess()
    {
      try
      {
        return _process.Start();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("GameLauncher: Error starting process {0}, {1} - {2}", _process.StartInfo.FileName, ex, ex.Message);
      }
      return false;
    }

    private void process_Exited(object sender, EventArgs e)
    {
      OnGameExited(e);
    }

    static string CreateArguments(string emulatorArguments, string gamePath)
    {
      string arguments;
      if (emulatorArguments.Contains(WILDCARD_GAME_PATH))
      {
        arguments = emulatorArguments.Replace(WILDCARD_GAME_PATH, gamePath);
      }
      else
      {
        arguments = string.Format("{0} {1}", emulatorArguments.TrimEnd(), gamePath);
      }
      return arguments;
    }
  }
}
