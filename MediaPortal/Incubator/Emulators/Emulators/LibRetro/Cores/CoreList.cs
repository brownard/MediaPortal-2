using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  class CoreUrl
  {
    public string Name { get; set; }
    public string Url { get; set; }
  }

  class CoreList : IHtmlDeserializable
  {
    protected const string URLS_REGEX_PATTERN = @"<td[^>]*><a href='([^']*?\.zip)'[^>]*>([^<]*)";
    protected static readonly Regex URLS_REGEX = new Regex(URLS_REGEX_PATTERN);

    protected List<CoreUrl> _coreUrls = new List<CoreUrl>();

    public List<CoreUrl> CoreUrls
    {
      get { return _coreUrls; }
    }

    public bool Deserialize(string html)
    {
      MatchCollection matches = URLS_REGEX.Matches(html);
      if (matches.Count == 0)
        return false;

      foreach (Match match in matches)
        _coreUrls.Add(new CoreUrl { Url = match.Groups[1].Value, Name = match.Groups[2].Value });
      return true;
    }
  }
}
