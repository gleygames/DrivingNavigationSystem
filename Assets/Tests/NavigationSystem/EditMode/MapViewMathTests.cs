using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewMathTests
    {
        private const float Tolerance = 0.001f;

        private MapViewMath math;

        [SetUp]
        public void SetUp()
        {
            math = new MapViewMath();
        }

        [Test]
        public void MapToViewport_Center_IsPivot()
        {
            Vector2 centerMap = new Vector2(150f, 80f);
            Vector2 pivot = new Vector2(25f, -10f);
            MapViewPose pose = math.ComputeContainerPose(centerMap, 40f, 2f, pivot);

            Vector2 viewportPoint = math.MapToViewport(centerMap, pose);

            Assert.AreEqual(pivot.x, viewportPoint.x, Tolerance);
            Assert.AreEqual(pivot.y, viewportPoint.y, Tolerance);
        }

        [Test]
        public void ViewportToMap_IsInverse_Rotated37()
        {
            Vector2 centerMap = new Vector2(100f, 50f);
            MapViewPose pose = math.ComputeContainerPose(centerMap, 37f, 1.5f, Vector2.zero);
            Vector2 originalMap = new Vector2(120f, 30f);

            Vector2 viewportPoint = math.MapToViewport(originalMap, pose);
            Vector2 roundTrip = math.ViewportToMap(viewportPoint, pose);

            Assert.AreEqual(originalMap.x, roundTrip.x, Tolerance);
            Assert.AreEqual(originalMap.y, roundTrip.y, Tolerance);
        }

        [Test]
        public void Scale_300mAcross600UnitsViewport_Is2()
        {
            float scale = math.ComputeScale(600f, 300f);

            Assert.AreEqual(2f, scale, Tolerance);
        }
    }
}
