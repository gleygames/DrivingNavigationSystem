using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class OffScreenArrowMathTests
    {
        private OffScreenArrowMath math;

        [SetUp]
        public void SetUp()
        {
            math = new OffScreenArrowMath();
        }

        [Test]
        public void Inside_ReturnsFalse()
        {
            Vector2 edgePoint;
            float angleDegrees;
            bool outside = math.ComputeEdgePoint(new Vector2(10f, 10f), new Vector2(100f, 100f), EdgeShape.Rectangle, 0f, out edgePoint, out angleDegrees);

            Assert.IsFalse(outside);
        }

        [Test]
        public void Circle_TargetRight_EdgeAtRadiusMinusInset_Angle90()
        {
            Vector2 edgePoint;
            float angleDegrees;
            bool outside = math.ComputeEdgePoint(new Vector2(200f, 0f), new Vector2(100f, 100f), EdgeShape.Circle, 8f, out edgePoint, out angleDegrees);

            Assert.IsTrue(outside);
            Assert.AreEqual(92f, edgePoint.x, 0.01f);
            Assert.AreEqual(0f, edgePoint.y, 0.01f);
            Assert.AreEqual(90f, angleDegrees, 0.01f);
        }

        [Test]
        public void Rect_TargetDiagonal_OnRectangleEdge()
        {
            Vector2 edgePoint;
            float angleDegrees;
            bool outside = math.ComputeEdgePoint(new Vector2(200f, 100f), new Vector2(100f, 50f), EdgeShape.Rectangle, 0f, out edgePoint, out angleDegrees);

            Assert.IsTrue(outside);
            Assert.AreEqual(100f, edgePoint.x, 0.01f);
            Assert.AreEqual(50f, edgePoint.y, 0.01f);
        }
    }
}
