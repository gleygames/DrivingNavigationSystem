using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class FullMapInteractionSettingsTests
    {
        [Test]
        public void TapTarget_DefaultIsMapAndMarkers()
        {
            FullMapInteractionSettings settings = new FullMapInteractionSettings();

            Assert.AreEqual(FullMapTapTarget.MapAndMarkers, settings.TapTarget);
        }

        [Test]
        public void SetTapTarget_ChangesValue()
        {
            FullMapInteractionSettings settings = new FullMapInteractionSettings();

            settings.SetTapTarget(FullMapTapTarget.MarkersOnly);

            Assert.AreEqual(FullMapTapTarget.MarkersOnly, settings.TapTarget);
        }
    }
}
