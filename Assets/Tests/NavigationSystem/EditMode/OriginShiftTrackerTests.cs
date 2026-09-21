using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class OriginShiftTrackerTests
    {
        [Test]
        public void Rectangle_ShiftIsCurrentMinusStored()
        {
            OriginShiftTracker tracker = new OriginShiftTracker(ShiftSource.Rectangle);
            Vector3 current = new Vector3(120f, 0f, 40f);
            Vector3 stored = new Vector3(100f, 0f, 30f);

            tracker.UpdateFromRectangle(current, stored);

            Assert.AreEqual(current - stored, tracker.Shift);
        }

        [Test]
        public void Manual_DeltasAccumulate_AndSurviveResetForNewMap()
        {
            OriginShiftTracker tracker = new OriginShiftTracker(ShiftSource.Manual);

            tracker.AddManualDelta(new Vector3(10f, 0f, 0f));
            tracker.AddManualDelta(new Vector3(0f, 0f, 5f));
            tracker.ResetForNewMap();

            Assert.AreEqual(new Vector3(10f, 0f, 5f), tracker.Shift);
        }
    }
}
