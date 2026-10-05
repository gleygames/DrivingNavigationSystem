using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class MinimapFollowSettingsTests
    {
        [Test]
        public void Defaults_MatchDesign()
        {
            MinimapFollowSettings settings = new MinimapFollowSettings();

            Assert.AreEqual(MinimapRotationMode.HeadingUp, settings.RotationMode);
            Assert.AreEqual(0.3f, settings.CarOffsetFromBottom, 0.001f);
            Assert.IsTrue(settings.SpeedZoom);
            Assert.AreEqual(150f, settings.SpeedZoomMinMeters, 0.001f);
            Assert.AreEqual(500f, settings.SpeedZoomMaxMeters, 0.001f);
            Assert.AreEqual(300f, settings.FixedZoomMeters, 0.001f);
        }

        [Test]
        public void ToggleRotationMode_SwitchesBothWays()
        {
            MinimapFollowSettings settings = new MinimapFollowSettings();

            settings.ToggleRotationMode();
            Assert.AreEqual(MinimapRotationMode.NorthUp, settings.RotationMode);

            settings.ToggleRotationMode();
            Assert.AreEqual(MinimapRotationMode.HeadingUp, settings.RotationMode);
        }
    }
}
