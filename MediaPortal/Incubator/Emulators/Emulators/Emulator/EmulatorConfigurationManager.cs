using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Emulator
{
  public class EmulatorConfigurationManager
  {
    List<EmulatorConfiguration> configurations;

    public EmulatorConfigurationManager()
    {
      var filters = new List<EmulatorFilter>(new[] { new EmulatorFilter() { Path = "p", PathFilter = "f" }, new EmulatorFilter() { Path = "p1", PathFilter = "f1" } });
      configurations = new List<EmulatorConfiguration>();
      configurations.Add(new EmulatorConfiguration() { Arguments = "arg1", Path = "path1", Name = "1", UseQuotes = true, WorkingDirectory = "w1", Filters = filters });
      configurations.Add(Project64.Create());
    }

    public void Serialize()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      IServerController sc = scm.ServerController;
      if (cd == null || sc == null)
        return;
      ICollection<Share> allShares = cd.GetShares(null, SharesFilter.All);
      
      XmlSerializer serializer = new XmlSerializer(typeof(List<EmulatorConfiguration>));
      StringBuilder sb = new StringBuilder();
      XmlWriter wr = XmlWriter.Create(sb);
      serializer.Serialize(wr, configurations);
      string output = sb.ToString();
      XmlReader r = XmlReader.Create(new StringReader(output));
      List<EmulatorConfiguration> newConfigs = (List<EmulatorConfiguration>)serializer.Deserialize(r);
      return;
    }

    public List<EmulatorConfiguration> DeSerialize()
    {
      XmlSerializer serializer = new XmlSerializer(typeof(List<EmulatorConfiguration>));
      return (List<EmulatorConfiguration>)serializer.Deserialize((Stream)null);
    }

    public EmulatorConfiguration GetConfiguration(string mimeType, string path)
    {
      EmulatorConfiguration configuration = null;
      if (!string.IsNullOrEmpty(mimeType))
        configuration = configurations.FirstOrDefault(c => c.Filters.Any(f => f.MimeType == mimeType));
      if (configuration == null)
        configuration = configurations.FirstOrDefault(c => c.Filters.Any(f => path.StartsWith(f.Path)));
      return configuration;
    }
  }
}
