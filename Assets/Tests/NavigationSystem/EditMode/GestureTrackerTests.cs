using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class GestureTrackerTests
    {
        private const float Tolerance = 0.001f;

        private FakeMapGestureTarget target;
        private GestureTracker tracker;

        [SetUp]
        public void SetUp()
        {
            target = new FakeMapGestureTarget();
            tracker = new GestureTracker(target);
        }

        [Test]
        public void SingleDrag_Pans()
        {
            tracker.PointerDown(0, new Vector2(0f, 0f), 0f);
            tracker.PointerMove(0, new Vector2(10f, 5f), 0.016f);

            Assert.AreEqual(1, target.PanCalls.Count);
            Assert.AreEqual(new Vector2(10f, 5f), target.PanCalls[0]);
        }

        [Test]
        public void Pinch_ZoomsAroundMidpoint()
        {
            tracker.PointerDown(0, new Vector2(0f, 0f), 0f);
            tracker.PointerDown(1, new Vector2(100f, 0f), 0f);

            tracker.PointerMove(0, new Vector2(-10f, 0f), 0.016f);

            Assert.AreEqual(1, target.ZoomCalls.Count);
            Assert.AreEqual(1.1f, target.ZoomCalls[0].Factor, Tolerance);
            Assert.AreEqual(new Vector2(45f, 0f), target.ZoomCalls[0].Pivot);
            Assert.AreEqual(1, target.PanCalls.Count);
            Assert.AreEqual(new Vector2(-5f, 0f), target.PanCalls[0]);
        }

        [Test]
        public void Scroll_OneNotch_Zoom1_25()
        {
            tracker.Scroll(1f, new Vector2(20f, 20f));

            Assert.AreEqual(1, target.ZoomCalls.Count);
            Assert.AreEqual(1.25f, target.ZoomCalls[0].Factor, Tolerance);
            Assert.AreEqual(new Vector2(20f, 20f), target.ZoomCalls[0].Pivot);
        }

        [Test]
        public void Tap_DoubleTapOn_WaitsThenTaps()
        {
            tracker.PointerDown(0, new Vector2(5f, 5f), 0f);
            tracker.PointerUp(0, new Vector2(5f, 5f), 0f, true);

            tracker.UpdateGestureLogic(0.1f, 0.1f);
            Assert.AreEqual(0, target.TapCalls.Count);

            tracker.UpdateGestureLogic(0.3f, 0.2f);
            Assert.AreEqual(1, target.TapCalls.Count);
            Assert.AreEqual(new Vector2(5f, 5f), target.TapCalls[0]);
        }

        [Test]
        public void DoubleTap_ZoomsIn_NoTap()
        {
            tracker.PointerDown(0, new Vector2(5f, 5f), 0f);
            tracker.PointerUp(0, new Vector2(5f, 5f), 0f, true);

            tracker.PointerDown(0, new Vector2(8f, 5f), 0.1f);
            tracker.PointerUp(0, new Vector2(8f, 5f), 0.1f, true);

            Assert.AreEqual(0, target.TapCalls.Count);
            Assert.AreEqual(1, target.ZoomCalls.Count);
            Assert.AreEqual(2f, target.ZoomCalls[0].Factor, Tolerance);
            Assert.AreEqual(new Vector2(8f, 5f), target.ZoomCalls[0].Pivot);

            tracker.UpdateGestureLogic(0.4f, 0.1f);
            Assert.AreEqual(0, target.TapCalls.Count);
        }

        [Test]
        public void DoubleTapOff_TapsImmediately()
        {
            tracker.DoubleTapEnabled = false;

            tracker.PointerDown(0, new Vector2(5f, 5f), 0f);
            tracker.PointerUp(0, new Vector2(5f, 5f), 0f, true);

            Assert.AreEqual(1, target.TapCalls.Count);
            Assert.AreEqual(new Vector2(5f, 5f), target.TapCalls[0]);
        }

        [Test]
        public void FastRelease_Flings_ThenStops()
        {
            tracker.PointerDown(0, new Vector2(0f, 0f), 0f);
            tracker.PointerMove(0, new Vector2(100f, 0f), 0.1f);
            tracker.PointerUp(0, new Vector2(100f, 0f), 0.1f, false);

            target.PanCalls.Clear();

            float time = 0.1f;
            for (int i = 0; i < 30; i++)
            {
                time += 0.05f;
                tracker.UpdateGestureLogic(time, 0.05f);
            }

            int callsAfter30Ticks = target.PanCalls.Count;
            Assert.Greater(callsAfter30Ticks, 0);
            Assert.Less(callsAfter30Ticks, 30);

            target.PanCalls.Clear();
            time += 0.05f;
            tracker.UpdateGestureLogic(time, 0.05f);

            Assert.AreEqual(0, target.PanCalls.Count);
        }

        [Test]
        public void SlowRelease_NoFling()
        {
            tracker.PointerDown(0, new Vector2(0f, 0f), 0f);
            tracker.PointerMove(0, new Vector2(1f, 0f), 0.1f);
            tracker.PointerUp(0, new Vector2(1f, 0f), 0.1f, false);

            target.PanCalls.Clear();

            tracker.UpdateGestureLogic(0.15f, 0.05f);

            Assert.AreEqual(0, target.PanCalls.Count);
        }

        [Test]
        public void NewPointerDown_StopsFling()
        {
            tracker.PointerDown(0, new Vector2(0f, 0f), 0f);
            tracker.PointerMove(0, new Vector2(100f, 0f), 0.1f);
            tracker.PointerUp(0, new Vector2(100f, 0f), 0.1f, false);

            tracker.PointerDown(1, new Vector2(50f, 50f), 0.12f);

            target.PanCalls.Clear();
            tracker.UpdateGestureLogic(0.2f, 0.08f);

            Assert.AreEqual(0, target.PanCalls.Count);
        }
    }
}
