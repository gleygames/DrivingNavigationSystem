using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineDataTests
    {
        private const float Tolerance = 0.01f;

        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RouteRequest request;
        private Route route;
        private MapFrame frame;
        private RouteLineData converter;
        private List<Vector2> points;
        private List<float> distances;
        private List<bool> dashed;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            request = new RouteRequest();
            route = new Route();
            frame = new MapFrame(new Vector3(150f, 0f, 0f), new Vector2(400f, 200f), 0f);
            converter = new RouteLineData();
            points = new List<Vector2>();
            distances = new List<float>();
            dashed = new List<bool>();
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

        [Test]
        public void Convert_NoDottedPiece_WhenOnRoad()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(250f, 0f, 0f));
            pathfinder.FindRoute(request, route);
            Assert.IsTrue(route.Success);

            converter.Convert(route, frame, points, distances, dashed);

            Assert.AreEqual(4, points.Count);
            Assert.AreEqual(3, dashed.Count);
            for (int i = 0; i < dashed.Count; i++)
            {
                Assert.IsFalse(dashed[i]);
            }
        }

        [Test]
        public void Convert_AppendsDottedPiece_WhenDestinationOffRoad()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(250f, 0f, 5f));
            pathfinder.FindRoute(request, route);
            Assert.IsTrue(route.Success);

            converter.Convert(route, frame, points, distances, dashed);

            Assert.AreEqual(5, points.Count);
            Assert.AreEqual(4, dashed.Count);
            Assert.IsFalse(dashed[0]);
            Assert.IsFalse(dashed[1]);
            Assert.IsFalse(dashed[2]);
            Assert.IsTrue(dashed[3]);
            Vector2 expectedEnd = frame.TrueToMap(new Vector3(250f, 0f, 5f));
            Assert.AreEqual(expectedEnd.x, points[4].x, Tolerance);
            Assert.AreEqual(expectedEnd.y, points[4].y, Tolerance);
        }

        [Test]
        public void Convert_DistancesMatchRouteLength()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            request.Set(new Vector3(50f, 0f, 0f), new Vector3(250f, 0f, 0f));
            pathfinder.FindRoute(request, route);
            Assert.IsTrue(route.Success);

            converter.Convert(route, frame, points, distances, dashed);

            Assert.AreEqual(0f, distances[0], Tolerance);
            Assert.AreEqual(route.Length, distances[distances.Count - 1], Tolerance);
        }

        private Pathfinder CreatePathfinder(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            return new Pathfinder(data);
        }
    }
}
