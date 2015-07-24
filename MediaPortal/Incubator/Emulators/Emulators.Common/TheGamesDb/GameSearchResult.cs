using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Emulators.Common.TheGamesDb
{
  class GameSearchResult
  {
    public int Id { get; set; }
    public string GameTitle { get; set; }
    public string Platform { get; set; }
    public DateTime? ReleaseDate { get; set; }

    public static GameSearchResult Create(XmlNode xmlNode)
    {
      GameSearchResult result = new GameSearchResult();

      XmlNode node = xmlNode.SelectSingleNode("id");      
      int id;
      if (node != null && int.TryParse(node.InnerText, out id))
        result.Id = id;

      node = xmlNode.SelectSingleNode("GameTitle");
      if (node != null)
        result.GameTitle = node.InnerText;

      node = xmlNode.SelectSingleNode("Platform");
      if (node != null)
        result.Platform = node.InnerText;

      node = xmlNode.SelectSingleNode("ReleaseDate");
      DateTime date;
      if (node != null && DateTime.TryParse(node.InnerText, out date))
        result.ReleaseDate = date;

      return result;
    }
  }
}
