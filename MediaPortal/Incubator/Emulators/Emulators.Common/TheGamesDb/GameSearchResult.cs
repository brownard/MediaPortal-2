using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Common.TheGamesDb
{
  [XmlRootAttribute("Data")]
  public class GameSearchResults
  {
    [XmlElement("Game")]
    public GameSearchResult[] Results { get; set; }
  }

  public class GameSearchResult
  {
    [XmlElement("id")]
    public int Id { get; set; }
    public string GameTitle { get; set; }
    public string Platform { get; set; }
    public string ReleaseDate { get; set; }
  }
}
