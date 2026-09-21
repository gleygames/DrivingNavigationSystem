using System.Collections.Generic;
using NUnit.Framework;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadImporterDiscoveryTests
    {
        private List<IRoadImporter> result;
        private RoadImporterDiscovery discovery;

        [SetUp]
        public void SetUp()
        {
            result = new List<IRoadImporter>();
            discovery = new RoadImporterDiscovery();
        }

        [Test]
        public void Discover_FindsAvailableFakeImporter()
        {
            discovery.Discover(result);

            Assert.IsTrue(ContainsSourceTag(result, "DiscoverableAvailableImporter"));
        }

        [Test]
        public void Discover_SkipsUnavailableImporter()
        {
            discovery.Discover(result);

            Assert.IsFalse(ContainsSourceTag(result, "DiscoverableUnavailableImporter"));
        }

        private bool ContainsSourceTag(List<IRoadImporter> importers, string sourceTag)
        {
            for (int i = 0; i < importers.Count; i++)
            {
                if (importers[i].SourceTag == sourceTag)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
