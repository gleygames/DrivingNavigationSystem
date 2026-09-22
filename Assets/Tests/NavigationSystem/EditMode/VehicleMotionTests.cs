using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class VehicleMotionTests
    {
        private VehicleMotion motion;

        [SetUp]
        public void SetUp()
        {
            motion = new VehicleMotion();
        }

        [Test]
        public void Speed_IsDisplacementOverTime()
        {
            motion.Reset(Vector3.zero, Vector3.forward);

            motion.UpdateVehicleMotionLogic(new Vector3(3f, 0f, 4f), Vector3.forward, 2f);

            Assert.AreEqual(2.5f, motion.Speed, 0.001f);
        }

        [Test]
        public void DeltaTimeZero_NoChange()
        {
            motion.Reset(Vector3.zero, Vector3.forward);
            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 5f), Vector3.forward, 1f);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 100f), Vector3.forward, 0f);

            Assert.AreEqual(5f, motion.Speed, 0.001f);
            Assert.IsFalse(motion.Teleported);
        }

        [Test]
        public void Stopped_BelowThreshold()
        {
            motion.Reset(Vector3.zero, Vector3.forward);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 0.01f), Vector3.forward, 1f);

            Assert.IsTrue(motion.IsStopped);
        }

        [Test]
        public void Teleport_Over50m_FlagsAndResets()
        {
            motion.Reset(Vector3.zero, Vector3.forward);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 60f), Vector3.forward, 1f);

            Assert.IsTrue(motion.Teleported);
            Assert.IsFalse(motion.HasMovedOnce);
            Assert.AreEqual(0f, motion.Speed, 0.001f);
        }

        [Test]
        public void MovementHeading_BeforeFirstMove_IsNose()
        {
            Vector3 nose = new Vector3(1f, 0f, 1f).normalized;
            motion.Reset(Vector3.zero, nose);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 0.05f), nose, 1f);

            Assert.IsFalse(motion.HasMovedOnce);
            Assert.AreEqual(nose.x, motion.MovementHeading.x, 0.001f);
            Assert.AreEqual(nose.z, motion.MovementHeading.z, 0.001f);
        }

        [Test]
        public void MovementHeading_SlowMovement_KeepsLast()
        {
            motion.Reset(Vector3.zero, Vector3.forward);
            motion.UpdateVehicleMotionLogic(new Vector3(5f, 0f, 0f), Vector3.forward, 1f);

            motion.UpdateVehicleMotionLogic(new Vector3(5f, 0f, 0.05f), Vector3.forward, 1f);

            Assert.IsTrue(motion.HasMovedOnce);
            Assert.AreEqual(1f, motion.MovementHeading.x, 0.001f);
            Assert.AreEqual(0f, motion.MovementHeading.z, 0.001f);
        }

        [Test]
        public void NoseHeading_Vertical_KeepsLastValid()
        {
            motion.Reset(Vector3.zero, Vector3.forward);
            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 0.5f), Vector3.forward, 1f);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, 1f), Vector3.up, 1f);

            Assert.AreEqual(0f, motion.NoseHeading.x, 0.001f);
            Assert.AreEqual(1f, motion.NoseHeading.z, 0.001f);
        }

        [Test]
        public void Reversing_MovementOppositeNose_True()
        {
            motion.Reset(Vector3.zero, Vector3.forward);

            motion.UpdateVehicleMotionLogic(new Vector3(0f, 0f, -5f), Vector3.forward, 1f);

            Assert.IsTrue(motion.IsReversing);
        }
    }
}
