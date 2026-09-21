using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class DiscoverableAvailableImporter : IRoadImporter
    {
        public string SourceTag { get { return "DiscoverableAvailableImporter"; } }
        public string DisplayName { get { return "Discoverable Available Importer"; } }

        public bool IsAvailable()
        {
            return true;
        }

        public void Import(RoadImportContext context)
        {
        }
    }
}
