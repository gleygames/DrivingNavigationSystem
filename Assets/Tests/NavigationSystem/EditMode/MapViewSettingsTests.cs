using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewSettingsTests
    {
        [Test]
        public void DefaultConstructor_BothChannels_ShowsPreview()
        {
            MapViewSettings settings = new MapViewSettings();

            Assert.AreEqual(MapViewSettings.MinimapChannelBit | MapViewSettings.FullMapChannelBit, settings.ChannelMask);
            Assert.IsTrue(settings.ShowPreview);
        }

        [Test]
        public void Constructor_SetsChannelAndPreview()
        {
            MapViewSettings settings = new MapViewSettings(MapViewSettings.MinimapChannelBit, false, false);

            Assert.AreEqual(MapViewSettings.MinimapChannelBit, settings.ChannelMask);
            Assert.IsFalse(settings.ShowPreview);
            Assert.IsFalse(settings.ShowMarkerLabels);
        }

        [Test]
        public void Defaults_MinZoom50_EdgeInset8_ArrowsOn()
        {
            MapViewSettings settings = new MapViewSettings();

            Assert.AreEqual(50f, settings.MinZoomMeters, 0.001f);
            Assert.AreEqual(8f, settings.EdgeInset, 0.001f);
            Assert.IsTrue(settings.ShowOffScreenArrows);
            Assert.IsTrue(settings.ShowArrowDistance);
        }

        [Test]
        public void ShowMarkerLabels_DefaultTrue()
        {
            MapViewSettings settings = new MapViewSettings();

            Assert.IsTrue(settings.ShowMarkerLabels);
        }
    }
}
