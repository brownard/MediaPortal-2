using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Common
{
  [TestFixture]
  public class TestRelationshipExtraction
  {
    [Test]
    public void TestCreateExternalItemFilter()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02");

      IFilter seriesFilter = RelationshipExtractorUtils.CreateExternalItemFilter(aspects, ExternalIdentifierAspect.TYPE_SERIES);
      Assert.AreEqual(seriesFilter.ToString(),
        "ExternalIdentifier.Source EQ TVDB And ExternalIdentifier.Type EQ SERIES And ExternalIdentifier.Id EQ tvdb_01 Or ExternalIdentifier.Source EQ TVMAZE And ExternalIdentifier.Type EQ SERIES And ExternalIdentifier.Id EQ tvmaze_01");

      IFilter episodeFilter = RelationshipExtractorUtils.CreateExternalItemFilter(aspects, ExternalIdentifierAspect.TYPE_EPISODE);
      Assert.AreEqual(episodeFilter.ToString(),
        "ExternalIdentifier.Source EQ TVDB And ExternalIdentifier.Type EQ EPISODE And ExternalIdentifier.Id EQ tvdb_02 Or ExternalIdentifier.Source EQ TVMAZE And ExternalIdentifier.Type EQ EPISODE And ExternalIdentifier.Id EQ tvmaze_02");
    }

    [Test]
    public void TestCreateExternalItemIdentifiers()
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02");
      MediaItemAspect.AddOrUpdateExternalIdentifier(aspects, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02");
      
      var seriesIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(aspects, ExternalIdentifierAspect.TYPE_SERIES);
      List<string> expectedSeriesIdentifiers = new List<string>
      {
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, "tvdb_01"),
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, "tvmaze_01")
      };
      
      CollectionAssert.AreEqual(seriesIdentifiers, expectedSeriesIdentifiers);

      var episodeIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(aspects, ExternalIdentifierAspect.TYPE_EPISODE);
      List<string> expectedEpisodeIdentifiers = new List<string>
      {
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, "tvdb_02"),
        string.Format("{0} | {1} | {2}", ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, "tvmaze_02")
      };

      CollectionAssert.AreEqual(episodeIdentifiers, expectedEpisodeIdentifiers);
    }
  }
}
