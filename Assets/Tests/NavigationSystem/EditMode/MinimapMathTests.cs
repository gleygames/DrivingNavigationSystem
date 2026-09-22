using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MinimapMathTests
    {
        private const float Tolerance = 0.001f;
        private const float SlowSpeed = 20f / 3.6f;
        private const float FastSpeed = 100f / 3.6f;

        private MinimapMath math;

        [SetUp]
        public void SetUp()
        {
            math = new MinimapMath();
        }

        [Test]
        public void SpeedZoom_Slow_150()
        {
            float zoom = math.SpeedZoom(10f / 3.6f, 150f, 500f, SlowSpeed, FastSpeed);

            Assert.AreEqual(150f, zoom, Tolerance);
        }

        [Test]
        public void SpeedZoom_Fast_500()
        {
            float zoom = math.SpeedZoom(130f / 3.6f, 150f, 500f, SlowSpeed, FastSpeed);

            Assert.AreEqual(500f, zoom, Tolerance);
        }

        [Test]
        public void SpeedZoom_Between_Linear()
        {
            float zoom = math.SpeedZoom(60f / 3.6f, 150f, 500f, SlowSpeed, FastSpeed);

            Assert.AreEqual(325f, zoom, Tolerance);
        }

        [Test]
        public void MaxZoomToFitMap_RoundUsesDiameter()
        {
            float roundSquare = math.MaxZoomToFitMap(new Vector2(1000f, 800f), true, new Vector2(200f, 200f));
            float roundWide = math.MaxZoomToFitMap(new Vector2(1000f, 800f), true, new Vector2(400f, 200f));

            Assert.AreEqual(800f, roundSquare, Tolerance);
            Assert.AreEqual(1600f, roundWide, Tolerance);
        }

        [Test]
        public void ClampCenter_Round_InsideMap_Unchanged()
        {
            Vector2 desired = new Vector2(300f, 400f);

            Vector2 clamped = math.ClampCenter(desired, new Vector2(1000f, 800f), 100f, true, 30f, new Vector2(100f, 100f));

            Assert.AreEqual(desired.x, clamped.x, Tolerance);
            Assert.AreEqual(desired.y, clamped.y, Tolerance);
        }

        [Test]
        public void ClampCenter_Round_NearEdge_Clamped()
        {
            Vector2 clamped = math.ClampCenter(new Vector2(40f, 790f), new Vector2(1000f, 800f), 100f, true, 30f, new Vector2(100f, 100f));

            Assert.AreEqual(100f, clamped.x, Tolerance);
            Assert.AreEqual(700f, clamped.y, Tolerance);
        }

        [Test]
        public void ClampCenter_Rect_Rotated45_UsesDiagonalExtents()
        {
            float expectedExtent = (100f + 50f) * Mathf.Sqrt(0.5f);

            Vector2 clamped = math.ClampCenter(new Vector2(10f, 995f), new Vector2(1000f, 1000f), 50f, false, 45f, new Vector2(100f, 50f));

            Assert.AreEqual(expectedExtent, clamped.x, Tolerance);
            Assert.AreEqual(1000f - expectedExtent, clamped.y, Tolerance);
        }

        [Test]
        public void SmoothAngle_WrapsAround180()
        {
            float velocity = 0f;

            float result = math.SmoothAngle(170f, -170f, 0.1f, ref velocity, 0.05f);

            Assert.IsTrue(result > 170f || result < -170f, "Result " + result + " went the long way around.");
            Assert.GreaterOrEqual(result, -180f);
            Assert.LessOrEqual(result, 180f);
        }
    }
}
