using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MapFrameTests
    {
        [Test]
        public void TrueToMap_NoRotation_CornerIsOrigin()
        {
            MapFrame frame = new MapFrame(new Vector3(50f, 0f, 50f), new Vector2(100f, 100f), 0f);

            Vector2 corner = frame.TrueToMap(new Vector3(0f, 0f, 0f));
            Vector2 opposite = frame.TrueToMap(new Vector3(100f, 0f, 100f));

            Assert.AreEqual(0f, corner.x, 0.01f);
            Assert.AreEqual(0f, corner.y, 0.01f);
            Assert.AreEqual(100f, opposite.x, 0.01f);
            Assert.AreEqual(100f, opposite.y, 0.01f);
        }

        [Test]
        public void TrueToMap_Rotated90_SwapsAxesCorrectly()
        {
            MapFrame frame = new MapFrame(new Vector3(0f, 0f, 0f), new Vector2(100f, 50f), 90f);

            Vector2 map = frame.TrueToMap(new Vector3(10f, 0f, -20f));

            Assert.AreEqual(70f, map.x, 0.01f);
            Assert.AreEqual(35f, map.y, 0.01f);
        }

        [Test]
        public void MapToTrue_IsInverseOfTrueToMap_Rotated37Degrees()
        {
            MapFrame frame = new MapFrame(new Vector3(12f, 0f, -8f), new Vector2(60f, 40f), 37f);
            Vector3 truePos = new Vector3(5f, 2f, 9f);

            Vector2 mapPos = frame.TrueToMap(truePos);
            Vector3 result = frame.MapToTrue(mapPos, truePos.y);

            Assert.AreEqual(truePos.x, result.x, 0.01f);
            Assert.AreEqual(truePos.y, result.y, 0.01f);
            Assert.AreEqual(truePos.z, result.z, 0.01f);
        }

        [Test]
        public void HeadingToMapAngle_NorthIsZero_EastIs90()
        {
            MapFrame frame = new MapFrame(Vector3.zero, new Vector2(100f, 100f), 0f);

            float north = frame.HeadingToMapAngle(new Vector3(0f, 0f, 1f));
            float east = frame.HeadingToMapAngle(new Vector3(1f, 0f, 0f));

            Assert.AreEqual(0f, north, 0.01f);
            Assert.AreEqual(90f, east, 0.01f);
        }

        [Test]
        public void HeadingToMapAngle_RectangleRotated_IsRelativeToMapUp()
        {
            MapFrame frame = new MapFrame(Vector3.zero, new Vector2(100f, 100f), 30f);
            Vector3 trueDir = new Vector3(0.9660254f, 0f, 0.258819f);

            float angle = frame.HeadingToMapAngle(trueDir);

            Assert.AreEqual(45f, angle, 0.1f);
        }
    }
}
