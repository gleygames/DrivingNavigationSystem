using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RoadMatcherTests
    {
        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RoadMatcher matcher;
        private Vector3 carPosition;
        private int changedCount;
        private int leftCount;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            changedCount = 0;
            leftCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            if (data != null)
            {
                Object.DestroyImmediate(data);
                data = null;
            }
            matcher = null;
        }

        [Test]
        public void DriveAlongRoad_StaysOnRoad_DistanceIncreases()
        {
            CreateMatcher(testNetworks.Line(1, 100f));
            Place(new Vector3(5f, 0f, 0.5f), Vector3.right);

            float previousDistance = matcher.DistanceAlong;
            for (int x = 6; x <= 95; x++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0.5f), Vector3.right, false, false);

                Assert.IsTrue(matcher.IsOnRoad);
                Assert.AreEqual(0, matcher.RoadIndex);
                Assert.Greater(matcher.DistanceAlong, previousDistance);
                Assert.IsTrue(matcher.MovingForward);
                previousDistance = matcher.DistanceAlong;
            }

            Assert.AreEqual(95f, matcher.DistanceAlong, 0.001f);
        }

        [Test]
        public void DriveAcrossIntersection_SwitchesToStraightRoad()
        {
            CreateMatcher(testNetworks.Grid(2, 2, 100f));
            int startRoad = FindRoad(new Vector3(0f, 0f, 100f), new Vector3(100f, 0f, 100f));
            int straightRoad = FindRoad(new Vector3(100f, 0f, 100f), new Vector3(200f, 0f, 100f));

            Place(new Vector3(10f, 0f, 100f), Vector3.right);
            Assert.AreEqual(startRoad, matcher.RoadIndex);

            DriveTo(new Vector3(190f, 0f, 100f), 1f);

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.IsFalse(matcher.IsInFork);
            Assert.AreEqual(straightRoad, matcher.RoadIndex);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(0, leftCount);
        }

        [Test]
        public void Stopped_NoChange()
        {
            CreateMatcher(testNetworks.Line(1, 100f));
            Place(new Vector3(50f, 0f, 0f), Vector3.right);

            matcher.UpdateRoadMatchingLogic(new Vector3(60f, 0f, 0f), Vector3.right, true, false);

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.AreEqual(50f, matcher.DistanceAlong, 0.001f);
            Assert.IsFalse(matcher.EnteredRoad);
            Assert.IsFalse(matcher.LeftRoad);
            Assert.IsFalse(matcher.ChangedRoad);
        }

        [Test]
        public void LeaveSideways_BeyondLeaveThreshold_LeftRoadThenLost()
        {
            CreateMatcher(testNetworks.Line(1, 100f));
            Place(new Vector3(50f, 0f, 0f), Vector3.right);

            matcher.UpdateRoadMatchingLogic(new Vector3(51f, 0f, 14f), Vector3.right, false, false);

            Assert.IsTrue(matcher.LeftRoad);
            Assert.IsFalse(matcher.IsOnRoad);
            Assert.AreEqual(-1, matcher.RoadIndex);
        }

        [Test]
        public void ReturnToRoad_WithinBackThreshold_EnteredRoad()
        {
            CreateMatcher(testNetworks.Line(1, 100f));
            Place(new Vector3(50f, 0f, 0f), Vector3.right);
            matcher.UpdateRoadMatchingLogic(new Vector3(51f, 0f, 14f), Vector3.right, false, false);

            matcher.UpdateRoadMatchingLogic(new Vector3(52f, 0f, 12f), Vector3.right, false, false);
            Assert.IsFalse(matcher.IsOnRoad);
            Assert.IsFalse(matcher.EnteredRoad);

            matcher.UpdateRoadMatchingLogic(new Vector3(53f, 0f, 9f), Vector3.right, false, false);
            Assert.IsTrue(matcher.IsOnRoad);
            Assert.IsTrue(matcher.EnteredRoad);
            Assert.AreEqual(0, matcher.RoadIndex);
        }

        [Test]
        public void Hysteresis_BetweenBackAndLeave_NoFlicker()
        {
            CreateMatcher(testNetworks.Line(1, 100f));
            Place(new Vector3(20f, 0f, 0f), Vector3.right);

            for (int i = 0; i < 20; i++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(21f + i, 0f, GetOscillation(i, 11f, 9f)), Vector3.right, false, false);

                Assert.IsTrue(matcher.IsOnRoad);
                Assert.IsFalse(matcher.LeftRoad);
                Assert.IsFalse(matcher.EnteredRoad);
            }

            matcher.UpdateRoadMatchingLogic(new Vector3(45f, 0f, 14f), Vector3.right, false, false);
            Assert.IsFalse(matcher.IsOnRoad);

            for (int i = 0; i < 20; i++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(46f + i, 0f, GetOscillation(i, 11f, 12f)), Vector3.right, false, false);

                Assert.IsFalse(matcher.IsOnRoad);
                Assert.IsFalse(matcher.EnteredRoad);
            }
        }

        [Test]
        public void ParallelOneWays_HeadingPicksCorrectRoad()
        {
            CreateMatcher(testNetworks.ParallelRoads(200f, 4f, true, 8f));

            Place(new Vector3(50f, 0f, 1.5f), Vector3.left);
            Assert.AreEqual(1, matcher.RoadIndex);

            DriveTo(new Vector3(30f, 0f, 1.5f), 1f);
            Assert.AreEqual(1, matcher.RoadIndex);
            Assert.AreEqual(0, changedCount);

            matcher.Reset();
            Place(new Vector3(50f, 0f, 2.5f), Vector3.right);
            Assert.AreEqual(0, matcher.RoadIndex);
        }

        [Test]
        public void SmallAngleFork_KeepsCandidates_ResolvesWhenSeparated()
        {
            CreateMatcher(testNetworks.Fork(100f, 100f, 20f, 8f));
            Place(new Vector3(5f, 0f, 0f), Vector3.right);
            DriveTo(new Vector3(99f, 0f, 0f), 1f);
            Assert.AreEqual(0, matcher.RoadIndex);

            Vector3 forkPoint = new Vector3(100f, 0f, 0f);
            Vector3 branchDirection = new Vector3(100f, 0f, 20f).normalized;

            matcher.UpdateRoadMatchingLogic(forkPoint + branchDirection * 1f, branchDirection, false, false);
            matcher.UpdateRoadMatchingLogic(forkPoint + branchDirection * 2f, branchDirection, false, false);
            Assert.IsTrue(matcher.IsOnRoad);
            Assert.IsTrue(matcher.IsInFork);
            Assert.AreEqual(2, matcher.ForkCandidateCount);

            for (int s = 3; s <= 30; s++)
            {
                matcher.UpdateRoadMatchingLogic(forkPoint + branchDirection * s, branchDirection, false, false);
            }

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.IsFalse(matcher.IsInFork);
            Assert.AreEqual(1, matcher.RoadIndex);
        }

        [Test]
        public void FourExitIntersection_WorstScoredExitKept_TurnFound()
        {
            CreateMatcher(testNetworks.Star(100f, 8f, 10f, -15f, 30f, 60f));
            Place(new Vector3(-50f, 0f, 0f), Vector3.right);
            DriveTo(new Vector3(0.5f, 0f, 0f), 1f);
            Assert.IsTrue(matcher.IsInFork);
            Assert.AreEqual(4, matcher.ForkCandidateCount);

            float radians = 60f * Mathf.Deg2Rad;
            Vector3 turnDirection = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
            DriveTo(turnDirection * 6f, 0.5f);

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.AreEqual(FindRoad(Vector3.zero, turnDirection * 100f), matcher.RoadIndex);
        }

        [Test]
        public void BridgeCrossing_StaysOnBridgeRoad()
        {
            CreateMatcher(testNetworks.Crossing(100f, 10f, 8f));
            Place(new Vector3(-50f, 0f, 0f), Vector3.right);
            Assert.AreEqual(0, matcher.RoadIndex);

            DriveTo(new Vector3(50f, 0f, 0f), 1f);

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.AreEqual(0, matcher.RoadIndex);
            Assert.AreEqual(0, changedCount);
            Assert.AreEqual(0, leftCount);
        }

        [Test]
        public void WrongInitialMatch_UnconnectedBetterFor10m_Switches()
        {
            CreateMatcher(testNetworks.ParallelRoads(200f, 5f, false, 8f));
            Place(new Vector3(10f, 0f, 1.5f), Vector3.right);
            Assert.AreEqual(0, matcher.RoadIndex);

            carPosition = new Vector3(11f, 0f, 5f);
            matcher.UpdateRoadMatchingLogic(carPosition, Vector3.right, false, false);
            DriveTo(new Vector3(20f, 0f, 5f), 1f);
            Assert.AreEqual(0, matcher.RoadIndex);

            DriveTo(new Vector3(60f, 0f, 5f), 1f);

            Assert.IsTrue(matcher.IsOnRoad);
            Assert.AreEqual(1, matcher.RoadIndex);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(0, leftCount);
        }

        [Test]
        public void Teleport_BecomesLost_ThenFindsNewRoad()
        {
            CreateMatcher(testNetworks.Grid(2, 2, 100f));
            int targetRoad = FindRoad(new Vector3(0f, 0f, 100f), new Vector3(0f, 0f, 200f));
            Place(new Vector3(50f, 0f, 0.5f), Vector3.right);
            Assert.IsTrue(matcher.IsOnRoad);

            matcher.UpdateRoadMatchingLogic(new Vector3(500f, 0f, 500f), Vector3.forward, true, true);
            Assert.IsFalse(matcher.IsOnRoad);
            Assert.AreEqual(-1, matcher.RoadIndex);

            matcher.UpdateRoadMatchingLogic(new Vector3(0.5f, 0f, 150f), Vector3.forward, true, true);
            Assert.IsTrue(matcher.IsOnRoad);
            Assert.IsTrue(matcher.EnteredRoad);
            Assert.AreEqual(targetRoad, matcher.RoadIndex);
        }

        [Test]
        public void FindAllWithin_OnePointPerRoad()
        {
            data = testNetworks.BuildNetwork(testNetworks.ParallelRoads(200f, 5f, false, 8f));
            RoadQuery query = new RoadQuery(data);
            List<RoadPoint> output = new List<RoadPoint>(16);

            query.FindAllWithin(new Vector3(50f, 0f, 1f), 20f, output);

            Assert.AreEqual(2, output.Count);
            int firstRoadHits = 0;
            int secondRoadHits = 0;
            for (int i = 0; i < output.Count; i++)
            {
                if (output[i].RoadIndex == 0)
                {
                    firstRoadHits++;
                    Assert.AreEqual(1f, output[i].Distance, 0.001f);
                }
                if (output[i].RoadIndex == 1)
                {
                    secondRoadHits++;
                    Assert.AreEqual(4f, output[i].Distance, 0.001f);
                }
            }
            Assert.AreEqual(1, firstRoadHits);
            Assert.AreEqual(1, secondRoadHits);
        }

        private void CreateMatcher(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            matcher = new RoadMatcher(data);
        }

        private void Place(Vector3 position, Vector3 heading)
        {
            carPosition = position;
            matcher.UpdateRoadMatchingLogic(position, heading, false, false);
        }

        private int FindRoad(Vector3 start, Vector3 end)
        {
            for (int r = 0; r < data.RoadCount; r++)
            {
                RoadRecord road = data.GetRoad(r);
                Vector3 first = data.GetPoint(road.FirstPoint);
                Vector3 last = data.GetPoint(road.FirstPoint + road.PointCount - 1);
                if (Vector3.Distance(first, start) < 0.01f && Vector3.Distance(last, end) < 0.01f)
                {
                    return r;
                }
            }
            return -1;
        }

        private void DriveTo(Vector3 target, float step)
        {
            Vector3 start = carPosition;
            Vector3 delta = target - start;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            Vector3 heading = delta / distance;
            int steps = Mathf.CeilToInt(distance / step);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 position = Vector3.Lerp(start, target, (float)i / steps);
                matcher.UpdateRoadMatchingLogic(position, heading, false, false);
                if (matcher.ChangedRoad)
                {
                    changedCount++;
                }
                if (matcher.LeftRoad)
                {
                    leftCount++;
                }
            }
            carPosition = target;
        }

        private float GetOscillation(int index, float even, float odd)
        {
            if (index % 2 == 0)
            {
                return even;
            }
            return odd;
        }
    }
}
