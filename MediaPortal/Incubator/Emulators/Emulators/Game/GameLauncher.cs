using Emulators.Common;
using Emulators.Emulator;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Game
{
  public class GameLauncher
  {
    const string WILDCARD_GAME_PATH = "<gamepath>";

    protected Process _process;
    protected EmulatorConfigurationManager _emulatorConfigurationManager;
    protected EmulatorConfiguration _emulatorConfiguration;
    protected string _gamePath;

    public static void LaunchGame(MediaItem mediaItem)
    {
      MediaItemAspect aspect;
      if (mediaItem == null || !mediaItem.Aspects.TryGetValue(MediaAspect.ASPECT_ID, out aspect))
        return;

      string mimeType = (string)aspect[MediaAspect.ATTR_MIME_TYPE];
      var locator = mediaItem.GetResourceLocator();
      EmulatorConfigurationManager manager = new EmulatorConfigurationManager();
      EmulatorConfiguration emulatorConfiguration = manager.GetConfiguration(mimeType, locator.NativeResourcePath.LastPathSegment.Path);
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
      _process = new Process();
      _process.StartInfo = new ProcessStartInfo(_emulatorConfiguration.Path, CreateArguments(_emulatorConfiguration, _gamePath));
      _process.StartInfo.WorkingDirectory = string.IsNullOrEmpty(_emulatorConfiguration.WorkingDirectory) ? DosPathHelper.GetDirectory(_emulatorConfiguration.Path) : _emulatorConfiguration.WorkingDirectory;
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

    static string CreateArguments(EmulatorConfiguration configuration, string gamePath)
    {
      if (configuration.UseQuotes)
        gamePath = string.Format("\"{0}\"", gamePath);

      string arguments = configuration.Arguments;
      if (string.IsNullOrEmpty(arguments))
        arguments = gamePath;
      else if (arguments.Contains(WILDCARD_GAME_PATH))
        arguments = arguments.Replace(WILDCARD_GAME_PATH, gamePath);
      else
        arguments = string.Format("{0} {1}", arguments.TrimEnd(), gamePath);
      return arguments;
    }
  }
}
