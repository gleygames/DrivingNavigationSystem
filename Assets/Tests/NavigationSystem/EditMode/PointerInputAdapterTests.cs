using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class PointerInputAdapterTests
    {
        private FullMapInteractionSettings settings;
        private PointerInputAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            settings = new FullMapInteractionSettings();
            adapter = new PointerInputAdapter(null, null, settings);
        }

        [Test]
        public void ApplySettings_FlingOff_TrackerFlingDisabled()
        {
            settings.SetFling(false);

            adapter.ApplySettings();

            Assert.IsFalse(adapter.Tracker.FlingEnabled);
        }

        [Test]
        public void ApplySettings_DoubleTapZoomOff_TrackerDoubleTapDisabled()
        {
            settings.SetDoubleTapZoom(false);

            adapter.ApplySettings();

            Assert.IsFalse(adapter.Tracker.DoubleTapEnabled);
        }

        [Test]
        public void ApplySettings_Defaults_BothEnabled()
        {
            adapter.ApplySettings();

            Assert.IsTrue(adapter.Tracker.FlingEnabled);
            Assert.IsTrue(adapter.Tracker.DoubleTapEnabled);
        }

        [Test]
        public void ApplySettings_Steps_Copied()
        {
            settings.SetMouseWheelStep(1.5f);
            settings.SetDoubleTapStep(3f);

            adapter.ApplySettings();

            Assert.AreEqual(1.5f, adapter.Tracker.MouseWheelStep, 0.001f);
            Assert.AreEqual(3f, adapter.Tracker.DoubleTapStep, 0.001f);
        }
    }
}
