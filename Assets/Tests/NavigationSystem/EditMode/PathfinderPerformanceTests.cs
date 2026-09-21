using Gley.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Gley.NavigationSystem.Tests
{
    public class PathfinderPerformanceTests
    {
        private const int RoadCount = 5000;
        private const int Seed = 7;
        private const int WarmupCount = 3;
        private const int PairCount = 200;
        private const int RoutesPerRun = 100;
        private const float MaxAverageMilliseconds = 2f;

        private Vector3[] fromPositions;
        private Vector3[] toPositions;
        private TestCityGenerator generator;
        private RoadNetworkBuildInput input;
        private RoadNetworkData data;
        private Pathfinder pathfinder;
        private RouteRequest request;
        private Route route;

        [SetUp]
        public void SetUp()
        {
            TestNetworks testNetworks = new TestNetworks();
            generator = new TestCityGenerator();
            input = generator.Generate(RoadCount, Seed);
            data = testNetworks.BuildNetwork(input);
            pathfinder = new Pathfinder(data);
            request = new RouteRequest();
            route = new Route();
            BuildRandomPairs();
        }

        [Test]
        public void FindRoute_5000Roads_AllocatesNoGarbageAfterWarmup()
        {
            for (int i = 0; i < WarmupCount; i++)
            {
                Run100Routes();
            }

            Assert.That(new TestDelegate(Run100Routes), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void FindRoute_5000Roads_AverageUnder2Milliseconds()
        {
            for (int i = 0; i < WarmupCount; i++)
            {
                Run100Routes();
            }

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < PairCount; i++)
            {
                FindRouteForPair(i);
            }
            stopwatch.Stop();

            float averageMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds / PairCount;
            CustomLogger.Log("PathfinderPerformanceTests: average route request " + averageMilliseconds + " ms over " + PairCount + " pairs on a " + data.RoadCount + "-road network.");
            Assert.Less(averageMilliseconds, MaxAverageMilliseconds);
        }

        [Test]
        public void TestCityGenerator_5000_ProducesBetween4500And5500Roads()
        {
            Assert.GreaterOrEqual(input.Roads.Count, 4500);
            Assert.LessOrEqual(input.Roads.Count, 5500);
        }

        private void BuildRandomPairs()
        {
            System.Random random = new System.Random(Seed);
            fromPositions = new Vector3[PairCount];
            toPositions = new Vector3[PairCount];
            for (int i = 0; i < PairCount; i++)
            {
                fromPositions[i] = RandomIntersectionPosition(random);
                toPositions[i] = RandomIntersectionPosition(random);
            }
        }

        private Vector3 RandomIntersectionPosition(System.Random random)
        {
            int index = random.Next(0, data.IntersectionCount);
            return data.GetIntersection(index).Position;
        }

        private void Run100Routes()
        {
            for (int i = 0; i < RoutesPerRun; i++)
            {
                FindRouteForPair(i);
            }
        }

        private void FindRouteForPair(int index)
        {
            request.Set(fromPositions[index], toPositions[index]);
            pathfinder.FindRoute(request, route);
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
