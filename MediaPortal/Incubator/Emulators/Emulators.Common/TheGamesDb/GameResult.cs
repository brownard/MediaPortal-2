using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Emulators.Common.TheGamesDb
{
  [XmlRootAttribute("Data")]
  public class GameResult
  {
    [XmlElement("baseImgUrl")]
    public string BaseImgUrl { get; set; }
    [XmlElement("Game")]
    public Game[] Games { get; set; }
  }

  public class Game
  {
    [XmlElement("id")]
    public int Id { get; set; }
    public string GameTitle { get; set; }
    public string Platform { get; set; }
    public string ReleaseDate { get; set; }
    public string Overview { get; set; }
    public string ESRB { get; set; }
    public GameGenres Genres { get; set; }
    public string Players { get; set; }
    [XmlElement("Co-op")]
    public string Coop { get; set; }
    public string Youtube { get; set; }
    public string Publisher { get; set; }
    public string Developer { get; set; }
    public double Rating { get; set; }
    public GameImages Images { get; set; }
  }

  public class GameGenres
  {
    [XmlElement("genre")]
    public string[] Genres { get; set; }
  }

  public class GameImages
  {
    public SimilarGames Similar { get; set; }
    [XmlElement("fanart")]
    public GameImage[] Fanart { get; set; }
    [XmlElement("boxart")]
    public GameImageBoxart[] Boxart { get; set; }
    [XmlElement("banner")]
    public GameImageOriginal[] Banner { get; set; }
    [XmlElement("screenshot")]
    public GameImage[] Screenshot { get; set; }
    [XmlElement("clearlogo")]
    public GameImageOriginal[] ClearLogo { get; set; }
  }

  public class SimilarGames
  {
    public int SimilarCount { get; set; }
    [XmlElement("Game")]
    public SimilarGame[] Games { get; set; }
  }

  public class SimilarGame
  {
    [XmlElement("id")]
    public int Id { get; set; }
    public int PlatformId { get; set; }
  }

  public class GameImage
  {
    [XmlElement("original")]
    public GameImageOriginal Original { get; set; }
    [XmlElement("thumb")]
    public string Thumb { get; set; }
  }

  public class GameImageOriginal
  {
    [XmlAttribute("width")]
    public int Width { get; set; }
    [XmlAttribute("height")]
    public int Height { get; set; }
    [XmlText]
    public string Value { get; set; }
  }

  public class GameImageBoxart : GameImageOriginal
  {
    [XmlAttribute("side")]
    public string Side { get; set; }
    [XmlAttribute("thumb")]
    public string Thumb { get; set; }
  }
}
