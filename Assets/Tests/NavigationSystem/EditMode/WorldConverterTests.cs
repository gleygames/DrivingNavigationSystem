using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class WorldConverterTests
    {
        private WorldConverter converter;

        [SetUp]
        public void SetUp()
        {
            converter = new WorldConverter();
        }

        [Test]
        public void WorldToTrue_WithShiftAndScale_RemovesBoth()
        {
            converter.SetUnitsPerMeter(100f);
            converter.SetShift(new Vector3(1000f, 0f, 0f));

            Vector3 result = converter.WorldToTrue(new Vector3(1100f, 0f, 200f));

            Assert.AreEqual(1f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(2f, result.z, 0.001f);
        }

        [Test]
        public void TrueToWorld_IsInverseOfWorldToTrue()
        {
            converter.SetUnitsPerMeter(12.5f);
            converter.SetShift(new Vector3(37f, 5f, -18f));
            Vector3 world = new Vector3(120f, 8f, -60f);

            Vector3 truePos = converter.WorldToTrue(world);
            Vector3 result = converter.TrueToWorld(truePos);

            Assert.AreEqual(world.x, result.x, 0.001f);
            Assert.AreEqual(world.y, result.y, 0.001f);
            Assert.AreEqual(world.z, result.z, 0.001f);
        }

        [Test]
        public void SetUnitsPerMeter_Zero_KeepsOldValue()
        {
            converter.SetUnitsPerMeter(4f);

            LogAssert.ignoreFailingMessages = true;
            converter.SetUnitsPerMeter(0f);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(4f, converter.UnitsPerMeter, 0.001f);
        }
    }
}
