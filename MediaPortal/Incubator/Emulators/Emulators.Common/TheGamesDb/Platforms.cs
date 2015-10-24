using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Emulators.Common.TheGamesDb
{
  [XmlRoot("Data")]
  public class PlatformsList
  {
    [XmlElement("basePlatformUrl")]
    public string BasePlatformUrl { get; set; }
    [XmlArrayItem("Platform")]
    public Platform[] Platforms { get; set; }
  }

  public class Platform
  {
    [XmlElement("id")]
    public int Id { get; set; }
    [XmlElement("name")]
    public string Name { get; set; }
    [XmlElement("alias")]
    public string Alias { get; set; }
  }
}
