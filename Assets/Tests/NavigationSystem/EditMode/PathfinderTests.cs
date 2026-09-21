using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class PathfinderTests
    {
        private const int HighwayType = 1;
        private const int MainType = 2;
        private const int LocalType = 4;
        private const float Tolerance = 0.001f;

        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RoutePreferences preferences;
        private Route route;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            preferences = new RoutePreferences();
            route = new Route();
        }

        [Test]
        public void Line_FromFirstToLast_UsesAllRoadsForward()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(4, 100f));

            pathfinder.FindRouteBetweenIntersections(0, 4, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(4, route.Segments.Count);
            for (int i = 0; i < 4; i++)
            {
                RouteSegment segment = route.Segments[i];
                Assert.AreEqual(i, segment.RoadIndex);
                Assert.IsTrue(segment.Forward);
                Assert.AreEqual(0f, segment.FromDistance, Tolerance);
                Assert.AreEqual(100f, segment.ToDistance, Tolerance);
            }
            Assert.AreEqual(400f, route.Length, Tolerance);
        }

        [Test]
        public void Square_OppositeCorner_ShortestHasLength2Sides()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Square(100f));

            pathfinder.FindRouteBetweenIntersections(0, 2, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(200f, route.Length, Tolerance);
        }

        [Test]
        public void OneWay_AgainstDirection_TakesLongWayAround()
        {
            RoadNetworkBuildInput input = testNetworks.Square(100f);
            input.Roads[0].OneWay = true;
            Pathfinder pathfinder = CreatePathfinder(input);

            pathfinder.FindRouteBetweenIntersections(1, 0, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(3, route.Segments.Count);
            Assert.AreEqual(1, route.Segments[0].RoadIndex);
            Assert.AreEqual(2, route.Segments[1].RoadIndex);
            Assert.AreEqual(3, route.Segments[2].RoadIndex);
            Assert.AreEqual(300f, route.Length, Tolerance);
        }

        [Test]
        public void OneWay_OnlyPathAgainst_ReturnsNoPath()
        {
            RoadNetworkBuildInput input = testNetworks.Line(2, 100f);
            input.Roads[0].OneWay = true;
            Pathfinder pathfinder = CreatePathfinder(input);

            pathfinder.FindRouteBetweenIntersections(1, 0, preferences, route);

            Assert.IsFalse(route.Success);
            Assert.AreEqual(FailureReason.NoPath, route.Failure);
            Assert.AreEqual(0, route.Segments.Count);
        }

        [Test]
        public void Disconnected_ReturnsNoPath()
        {
            RoadNetworkBuildInput input = testNetworks.Line(1, 100f);
            AddIntersection(input, 3, 1000f, 0f);
            AddIntersection(input, 4, 1100f, 0f);
            AddRoad(input, 2, HighwayType, false, 3, 4);
            Pathfinder pathfinder = CreatePathfinder(input);

            pathfinder.FindRouteBetweenIntersections(0, 3, preferences, route);

            Assert.IsFalse(route.Success);
            Assert.AreEqual(FailureReason.NoPath, route.Failure);
        }

        [Test]
        public void Fastest_PrefersFasterLongerRoad()
        {
            Pathfinder pathfinder = CreatePathfinder(TwoRoutes(LocalType, HighwayType, 100f));
            preferences.Mode = RouteMode.Fastest;

            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(1, route.Segments[0].RoadIndex);
            Assert.AreEqual(2, route.Segments[1].RoadIndex);
        }

        [Test]
        public void Shortest_IgnoresSpeed()
        {
            Pathfinder pathfinder = CreatePathfinder(TwoRoutes(LocalType, HighwayType, 100f));
            preferences.Mode = RouteMode.Shortest;

            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(0, route.Segments[0].RoadIndex);
            Assert.AreEqual(100f, route.Length, Tolerance);
        }

        [Test]
        public void Avoid_Highway_TakesOtherRoadIfNotMuchLonger()
        {
            Pathfinder pathfinder = CreatePathfinder(TwoRoutes(HighwayType, LocalType, 30f));

            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            Assert.AreEqual(1, route.Segments.Count);

            preferences.SetPreference(HighwayType, RoadTypePreference.Avoid);
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(1, route.Segments[0].RoadIndex);
            Assert.AreEqual(2, route.Segments[1].RoadIndex);
        }

        [Test]
        public void Avoid_Highway_StillUsedWhenOnlyPath()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            preferences.SetPreference(HighwayType, RoadTypePreference.Avoid);

            pathfinder.FindRouteBetweenIntersections(0, 3, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(3, route.Segments.Count);
            Assert.AreEqual(300f, route.Length, Tolerance);
        }

        [Test]
        public void Prefer_Type_ChangesChoice()
        {
            Pathfinder pathfinder = CreatePathfinder(TwoRoutes(LocalType, MainType, 30f));

            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            Assert.AreEqual(1, route.Segments.Count);

            preferences.SetPreference(MainType, RoadTypePreference.Prefer);
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(1, route.Segments[0].RoadIndex);
            Assert.AreEqual(2, route.Segments[1].RoadIndex);
        }

        [Test]
        public void UTurnNever_AtIntersection_NotUsed()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRouteFromRoad(0, true, 0, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(4, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true);
            AssertSegment(route.Segments[1], 1, true);
            AssertSegment(route.Segments[2], 1, false);
            AssertSegment(route.Segments[3], 0, false);
            Assert.AreEqual(400f, route.Length, Tolerance);
        }

        [Test]
        public void UTurnAtIntersections_AllowsIt()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            preferences.UTurn = UTurnRule.AtIntersections;

            pathfinder.FindRouteFromRoad(0, true, 0, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(2, route.Segments.Count);
            AssertSegment(route.Segments[0], 0, true);
            AssertSegment(route.Segments[1], 0, false);
            Assert.AreEqual(200f, route.Length, Tolerance);
        }

        [Test]
        public void DeadEnd_AlwaysAllowsTurnAround()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            preferences.UTurn = UTurnRule.Never;

            pathfinder.FindRouteFromRoad(1, true, 0, preferences, route);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(3, route.Segments.Count);
            AssertSegment(route.Segments[0], 1, true);
            AssertSegment(route.Segments[1], 1, false);
            AssertSegment(route.Segments[2], 0, false);
            Assert.AreEqual(300f, route.Length, Tolerance);
        }

        [Test]
        public void Eta_IsLengthOverSpeedWithoutMultipliers()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            preferences.Mode = RouteMode.Fastest;
            preferences.SetPreference(HighwayType, RoadTypePreference.Avoid);

            pathfinder.FindRouteBetweenIntersections(0, 2, preferences, route);

            float speed = data.GetRoad(0).Speed;
            Assert.IsTrue(route.Success);
            Assert.AreEqual(200f / speed, route.Eta, Tolerance);
        }

        [Test]
        public void Result_MatchesDijkstra_OnRandomGrid()
        {
            System.Random random = new System.Random(42);
            RoadNetworkBuildInput input = testNetworks.Grid(10, 10, 100f);
            for (int i = 0; i < input.Roads.Count; i++)
            {
                BuildRoad road = input.Roads[i];
                road.TypeId = random.Next(1, 5);
                if (random.NextDouble() < 0.3)
                {
                    road.OneWay = true;
                    if (random.NextDouble() < 0.5)
                    {
                        int startId = road.StartIntersectionId;
                        road.StartIntersectionId = road.EndIntersectionId;
                        road.EndIntersectionId = startId;
                        road.Points.Reverse();
                    }
                }
            }
            Pathfinder pathfinder = CreatePathfinder(input);
            preferences.Mode = RouteMode.Fastest;
            preferences.SetPreference(MainType, RoadTypePreference.Avoid);
            preferences.SetPreference(3, RoadTypePreference.Prefer);

            int intersectionCount = data.IntersectionCount;
            for (int pair = 0; pair < 50; pair++)
            {
                int from = random.Next(0, intersectionCount);
                int to = random.Next(0, intersectionCount);

                float expected = Dijkstra(from, to);
                pathfinder.FindRouteBetweenIntersections(from, to, preferences, route);

                if (float.IsPositiveInfinity(expected))
                {
                    Assert.IsFalse(route.Success, "Pair " + from + " -> " + to);
                }
                else
                {
                    Assert.IsTrue(route.Success, "Pair " + from + " -> " + to);
                    Assert.AreEqual(expected, RouteCost(), 0.01f, "Pair " + from + " -> " + to);
                }
            }
        }

        private Pathfinder CreatePathfinder(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            return new Pathfinder(data);
        }

        private RoadNetworkBuildInput TwoRoutes(int directType, int detourType, float detourHeight)
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();
            AddIntersection(input, 1, 0f, 0f);
            AddIntersection(input, 2, 100f, 0f);
            AddIntersection(input, 3, 50f, detourHeight);
            AddRoad(input, 1, directType, false, 1, 2);
            AddRoad(input, 2, detourType, false, 1, 3);
            AddRoad(input, 3, detourType, false, 3, 2);
            return input;
        }

        private void AddIntersection(RoadNetworkBuildInput input, int id, float x, float z)
        {
            BuildIntersection intersection = new BuildIntersection();
            intersection.Id = id;
            intersection.Position = new Vector3(x, 0f, z);
            input.Intersections.Add(intersection);
        }

        private void AddRoad(RoadNetworkBuildInput input, int id, int typeId, bool oneWay, int startId, int endId)
        {
            BuildRoad road = new BuildRoad();
            road.Id = id;
            road.TypeId = typeId;
            road.OneWay = oneWay;
            road.StartIntersectionId = startId;
            road.EndIntersectionId = endId;
            road.Points.Add(FindIntersectionPosition(input, startId));
            road.Points.Add(FindIntersectionPosition(input, endId));
            input.Roads.Add(road);
        }

        private Vector3 FindIntersectionPosition(RoadNetworkBuildInput input, int id)
        {
            for (int i = 0; i < input.Intersections.Count; i++)
            {
                if (input.Intersections[i].Id == id)
                {
                    return input.Intersections[i].Position;
                }
            }
            return Vector3.zero;
        }

        private void AssertSegment(RouteSegment segment, int roadIndex, bool forward)
        {
            Assert.AreEqual(roadIndex, segment.RoadIndex);
            Assert.AreEqual(forward, segment.Forward);
        }

        private float CostPerMeter(int roadIndex)
        {
            RoadRecord road = data.GetRoad(roadIndex);
            return preferences.GetMultiplier(road.TypeId) / road.Speed;
        }

        private float RouteCost()
        {
            float cost = 0f;
            for (int i = 0; i < route.Segments.Count; i++)
            {
                RouteSegment segment = route.Segments[i];
                cost += Mathf.Abs(segment.ToDistance - segment.FromDistance) * CostPerMeter(segment.RoadIndex);
            }
            return cost;
        }

        private float Dijkstra(int from, int to)
        {
            int count = data.IntersectionCount;
            float[] distances = new float[count];
            bool[] done = new bool[count];
            for (int i = 0; i < count; i++)
            {
                distances[i] = float.PositiveInfinity;
            }
            distances[from] = 0f;

            for (int iteration = 0; iteration < count; iteration++)
            {
                int current = -1;
                for (int i = 0; i < count; i++)
                {
                    if (!done[i] && !float.IsPositiveInfinity(distances[i]))
                    {
                        if (current < 0 || distances[i] < distances[current])
                        {
                            current = i;
                        }
                    }
                }
                if (current < 0)
                {
                    break;
                }
                done[current] = true;

                for (int r = 0; r < data.RoadCount; r++)
                {
                    RoadRecord road = data.GetRoad(r);
                    float cost = road.Length * CostPerMeter(r);
                    if (road.StartIntersection == current)
                    {
                        Relax(distances, road.EndIntersection, distances[current] + cost);
                    }
                    if (road.EndIntersection == current && !road.OneWay)
                    {
                        Relax(distances, road.StartIntersection, distances[current] + cost);
                    }
                }
            }

            return distances[to];
        }

        private void Relax(float[] distances, int node, float cost)
        {
            if (cost < distances[node])
            {
                distances[node] = cost;
            }
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
