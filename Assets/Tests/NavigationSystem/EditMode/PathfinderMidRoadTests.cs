using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class PathfinderMidRoadTests
    {
        private const float Tolerance = 0.001f;

        private List<Vector3> points;
        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RouteRequest request;
        private Route route;

        [SetUp]
        public void SetUp()
        {
            points = new List<Vector3>();
            testNetworks = new TestNetworks();
            request = new RouteRequest();
            route = new Route();
        }

        [Test]
        public void MidRoad_ToMidRoad_FirstAndLastSegmentsArePartial()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(250f, 0f, 0f));

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.IsFalse(route.ArrivedImmediately);
            Assert.AreEqual(3, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true, 50f, 100f);
            AssertSegment(route.Segments[1], 1, true, 0f, 100f);
            AssertSegment(route.Segments[2], 2, true, 0f, 50f);
            Assert.AreEqual(200f, route.Length, Tolerance);
            Assert.AreEqual(0, route.Start.RoadIndex);
            Assert.AreEqual(50f, route.Start.DistanceAlong, Tolerance);
            Assert.AreEqual(2, route.End.RoadIndex);
            Assert.AreEqual(50f, route.End.DistanceAlong, Tolerance);
            Assert.AreEqual(new Vector3(250f, 0f, 0f), route.Destination);
        }

        [Test]
        public void SameRoad_DestinationAhead_SingleDirectSegment()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(30f, 0f, 0f), new Vector3(60f, 0f, 0f));
            request.SetHeading(new Vector3(1f, 0f, 0f));

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(1, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true, 30f, 60f);
            Assert.AreEqual(30f, route.Length, Tolerance);
        }

        [Test]
        public void SameRoad_DestinationBehind_UTurnNever_LoopsAround()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Square(100f));
            request.Set(new Vector3(60f, 0f, 0f), new Vector3(30f, 0f, 0f));
            request.SetHeading(new Vector3(1f, 0f, 0f));
            request.Preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(5, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true, 60f, 100f);
            AssertSegment(route.Segments[1], 1, true, 0f, 100f);
            AssertSegment(route.Segments[2], 2, true, 0f, 100f);
            AssertSegment(route.Segments[3], 3, true, 0f, 100f);
            AssertSegment(route.Segments[4], 0, true, 0f, 30f);
            Assert.AreEqual(370f, route.Length, Tolerance);
        }

        [Test]
        public void SameRoad_DestinationBehind_UTurnAnywhere_GoesBackDirectly()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Square(100f));
            request.Set(new Vector3(60f, 0f, 0f), new Vector3(30f, 0f, 0f));
            request.SetHeading(new Vector3(1f, 0f, 0f));
            request.Preferences.UTurn = UTurnRule.Anywhere;

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(1, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, false, 60f, 30f);
            Assert.AreEqual(30f, route.Length, Tolerance);
        }

        [Test]
        public void Heading_Backward_UTurnNever_StartsBackward()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Square(100f));
            request.Set(new Vector3(40f, 0f, 0f), new Vector3(100f, 0f, 50f));
            request.SetHeading(new Vector3(-1f, 0f, 0f));
            request.Preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(4, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, false, 40f, 0f);
            AssertSegment(route.Segments[1], 3, false, 100f, 0f);
            AssertSegment(route.Segments[2], 2, false, 100f, 0f);
            AssertSegment(route.Segments[3], 1, false, 100f, 50f);
            Assert.AreEqual(290f, route.Length, Tolerance);
        }

        [Test]
        public void NoHeading_EitherDirectionAllowed_PicksShorter()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Square(100f));
            request.Set(new Vector3(30f, 0f, 0f), new Vector3(0f, 0f, 50f));
            request.ClearHeading();
            request.Preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, false, 30f, 0f);
            AssertSegment(route.Segments[1], 3, false, 100f, 50f);
            Assert.AreEqual(80f, route.Length, Tolerance);
        }

        [Test]
        public void OneWay_WrongWayHeading_StartsInLegalDirection()
        {
            RoadNetworkBuildInput input = testNetworks.Square(100f);
            input.Roads[0].OneWay = true;
            Pathfinder pathfinder = CreatePathfinder(input);
            request.Set(new Vector3(40f, 0f, 0f), new Vector3(100f, 0f, 50f));
            request.SetHeading(new Vector3(-1f, 0f, 0f));
            request.Preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true, 40f, 100f);
            AssertSegment(route.Segments[1], 1, true, 0f, 50f);
            Assert.AreEqual(110f, route.Length, Tolerance);
        }

        [Test]
        public void ArrivedImmediately_WithinArrivalDistance()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(50f, 0f, 5f), new Vector3(55f, 0f, 0f));

            pathfinder.FindRoute(request, route);

            Assert.IsTrue(route.Success);
            Assert.IsTrue(route.ArrivedImmediately);
            Assert.AreEqual(FailureReason.None, route.Failure);
            Assert.AreEqual(0, route.Segments.Count);
        }

        [Test]
        public void NoRoadNearStart_FailureReason()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(50f, 0f, 300f), new Vector3(50f, 0f, 0f));

            pathfinder.FindRoute(request, route);

            Assert.IsFalse(route.Success);
            Assert.AreEqual(FailureReason.NoRoadNearStart, route.Failure);
            Assert.AreEqual(0, route.Segments.Count);
        }

        [Test]
        public void NoRoadNearDestination_FailureReason()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(50f, 0f, 100f));

            pathfinder.FindRoute(request, route);

            Assert.IsFalse(route.Success);
            Assert.AreEqual(FailureReason.NoRoadNearDestination, route.Failure);
            Assert.AreEqual(0, route.Segments.Count);
        }

        [Test]
        public void GetTruePoints_NoDuplicatePointsAtJoins()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(250f, 0f, 0f));

            pathfinder.FindRoute(request, route);
            route.GetTruePoints(points);

            Assert.AreEqual(4, points.Count);
            for (int i = 1; i < points.Count; i++)
            {
                Assert.Greater(Vector3.Distance(points[i - 1], points[i]), Tolerance);
            }
            AssertPoint(new Vector3(50f, 0f, 0f), points[0]);
            AssertPoint(new Vector3(100f, 0f, 0f), points[1]);
            AssertPoint(new Vector3(200f, 0f, 0f), points[2]);
            AssertPoint(new Vector3(250f, 0f, 0f), points[3]);
        }

        [Test]
        public void GetTruePoints_PartialRoadsCutAtDistances()
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();
            AddIntersection(input, 1, new Vector3(0f, 0f, 0f));
            AddIntersection(input, 2, new Vector3(100f, 0f, 100f));
            AddIntersection(input, 3, new Vector3(100f, 0f, 200f));
            BuildRoad bentRoad = AddRoad(input, 1, 1, 2);
            bentRoad.Points.Add(new Vector3(0f, 0f, 0f));
            bentRoad.Points.Add(new Vector3(100f, 0f, 0f));
            bentRoad.Points.Add(new Vector3(100f, 0f, 100f));
            BuildRoad straightRoad = AddRoad(input, 2, 2, 3);
            straightRoad.Points.Add(new Vector3(100f, 0f, 100f));
            straightRoad.Points.Add(new Vector3(100f, 0f, 200f));
            Pathfinder pathfinder = CreatePathfinder(input);
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(100f, 0f, 150f));

            pathfinder.FindRoute(request, route);
            route.GetTruePoints(points);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true, 50f, 200f);
            AssertSegment(route.Segments[1], 1, true, 0f, 50f);
            Assert.AreEqual(4, points.Count);
            AssertPoint(new Vector3(50f, 0f, 0f), points[0]);
            AssertPoint(new Vector3(100f, 0f, 0f), points[1]);
            AssertPoint(new Vector3(100f, 0f, 100f), points[2]);
            AssertPoint(new Vector3(100f, 0f, 150f), points[3]);
        }

        [Test]
        public void GetPoints_WithConverter_ReturnsWorldPositions()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(150f, 0f, 0f));
            WorldConverter converter = new WorldConverter();
            converter.SetUnitsPerMeter(2f);
            converter.SetShift(new Vector3(100f, 0f, 0f));

            pathfinder.FindRoute(request, route);
            route.SetConverter(converter);
            route.GetPoints(points);

            Assert.AreEqual(3, points.Count);
            AssertPoint(new Vector3(200f, 0f, 0f), points[0]);
            AssertPoint(new Vector3(300f, 0f, 0f), points[1]);
            AssertPoint(new Vector3(400f, 0f, 0f), points[2]);
        }

        private Pathfinder CreatePathfinder(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            return new Pathfinder(data);
        }

        private void AssertSegment(RouteSegment segment, int roadIndex, bool forward, float fromDistance, float toDistance)
        {
            Assert.AreEqual(roadIndex, segment.RoadIndex);
            Assert.AreEqual(forward, segment.Forward);
            Assert.AreEqual(fromDistance, segment.FromDistance, Tolerance);
            Assert.AreEqual(toDistance, segment.ToDistance, Tolerance);
        }

        private void AssertPoint(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance);
            Assert.AreEqual(expected.y, actual.y, Tolerance);
            Assert.AreEqual(expected.z, actual.z, Tolerance);
        }

        private void AddIntersection(RoadNetworkBuildInput input, int id, Vector3 position)
        {
            BuildIntersection intersection = new BuildIntersection();
            intersection.Id = id;
            intersection.Position = position;
            input.Intersections.Add(intersection);
        }

        private BuildRoad AddRoad(RoadNetworkBuildInput input, int id, int startId, int endId)
        {
            BuildRoad road = new BuildRoad();
            road.Id = id;
            road.TypeId = 1;
            road.OneWay = false;
            road.StartIntersectionId = startId;
            road.EndIntersectionId = endId;
            input.Roads.Add(road);
            return road;
        }

        [TearDown]
        public void TearDown()
        {
            if (data != null)
            {
                if (data.Settings != null)
                {
                    Object.DestroyImmediate(data.Settings);
                }
                Object.DestroyImmediate(data);
                data = null;
            }
        }
    }
}
