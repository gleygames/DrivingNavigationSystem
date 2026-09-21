using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class DiscoverableUnavailableImporter : IRoadImporter
    {
        public string SourceTag { get { return "DiscoverableUnavailableImporter"; } }
        public string DisplayName { get { return "Discoverable Unavailable Importer"; } }

        public bool IsAvailable()
        {
            return false;
        }

        public void Import(RoadImportContext context)
        {
        }
    }
}
