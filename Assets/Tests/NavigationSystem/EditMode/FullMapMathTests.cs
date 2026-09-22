using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class FullMapMathTests
    {
        private const float Tolerance = 0.001f;

        private FullMapMath math;

        [SetUp]
        public void SetUp()
        {
            math = new FullMapMath();
        }

        [Test]
        public void MaxZoom_Fit_WideScreenSquareMap_UsesHeight()
        {
            float zoom = math.MaxZoomMeters(new Vector2(1000f, 1000f), new Vector2(400f, 200f), true);

            Assert.AreEqual(2000f, zoom, Tolerance);
        }

        [Test]
        public void MaxZoom_Fill_WideScreenSquareMap_UsesWidth()
        {
            float zoom = math.MaxZoomMeters(new Vector2(1000f, 1000f), new Vector2(400f, 200f), false);

            Assert.AreEqual(1000f, zoom, Tolerance);
        }

        [Test]
        public void ClampCenter_ViewLargerThanMap_Centers()
        {
            Vector2 clamped = math.ClampCenter(new Vector2(700f, 700f), new Vector2(1000f, 800f), new Vector2(600f, 50f));

            Assert.AreEqual(500f, clamped.x, Tolerance);
            Assert.AreEqual(700f, clamped.y, Tolerance);
        }

        [Test]
        public void ZoomAroundPivot_PivotStaysFixed()
        {
            Vector2 center = new Vector2(200f, 150f);
            Vector2 pivotMap = new Vector2(260f, 150f);
            float oldZoom = 300f;
            float newZoom = 150f;
            float viewportWidth = 400f;

            Vector2 newCenter = math.ZoomAroundPivot(center, pivotMap, oldZoom, newZoom);

            float oldScale = viewportWidth / oldZoom;
            float newScale = viewportWidth / newZoom;
            float oldScreenX = (pivotMap.x - center.x) * oldScale;
            float newScreenX = (pivotMap.x - newCenter.x) * newScale;

            Assert.AreEqual(oldScreenX, newScreenX, Tolerance);
        }
    }
}
