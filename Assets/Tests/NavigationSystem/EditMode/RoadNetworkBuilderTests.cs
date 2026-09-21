using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class RoadNetworkBuilderTests
    {
        private TestNetworks testNetworks;
        private NavigationSettings settings;
        private RoadNetworkBuilder builder;
        private RoadNetworkData data;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
            builder = new RoadNetworkBuilder();
            data = ScriptableObject.CreateInstance<RoadNetworkData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Build_Line_ComputesLengthsIgnoringHeight()
        {
            RoadNetworkBuildInput input = testNetworks.Line(1, 10f);
            input.Roads[0].Points[0] = new Vector3(0f, 0f, 0f);
            input.Roads[0].Points[1] = new Vector3(10f, 5f, 0f);

            builder.Build(input, settings, data);

            Assert.AreEqual(10f, data.GetRoad(0).Length, 0.001f);
        }

        [Test]
        public void Build_Line_CumulativeDistancesStartAtZeroPerRoad()
        {
            RoadNetworkBuildInput input = testNetworks.Line(2, 10f);

            builder.Build(input, settings, data);

            RoadRecord firstRoad = data.GetRoad(0);
            RoadRecord secondRoad = data.GetRoad(1);
            Assert.AreEqual(0f, data.GetPointDistance(firstRoad.FirstPoint), 0.001f);
            Assert.AreEqual(10f, data.GetPointDistance(firstRoad.FirstPoint + 1), 0.001f);
            Assert.AreEqual(0f, data.GetPointDistance(secondRoad.FirstPoint), 0.001f);
            Assert.AreEqual(10f, data.GetPointDistance(secondRoad.FirstPoint + 1), 0.001f);
        }

        [Test]
        public void Build_Square_EachIntersectionHasTwoLinks()
        {
            RoadNetworkBuildInput input = testNetworks.Square(10f);

            builder.Build(input, settings, data);

            for (int i = 0; i < data.IntersectionCount; i++)
            {
                Assert.AreEqual(2, data.GetIntersection(i).LinkCount);
            }
        }

        [Test]
        public void Build_SpeedOverride_WinsOverType()
        {
            RoadNetworkBuildInput input = testNetworks.Line(1, 10f);
            input.Roads[0].SpeedOverride = 5f;

            builder.Build(input, settings, data);

            Assert.AreEqual(5f, data.GetRoad(0).Speed, 0.001f);
        }

        [Test]
        public void Build_MissingType_FallsBackToFirstType()
        {
            RoadNetworkBuildInput input = testNetworks.Line(1, 10f);
            input.Roads[0].TypeId = 999;

            builder.Build(input, settings, data);

            RoadType firstType = settings.RoadTypes[0];
            Assert.AreEqual(firstType.Id, data.GetRoad(0).TypeId);
            Assert.AreEqual(firstType.SpeedMetersPerSecond, data.GetRoad(0).Speed, 0.001f);
        }

        [Test]
        public void Build_MissingIntersection_SkipsRoadWithoutThrowing()
        {
            RoadNetworkBuildInput input = testNetworks.Line(1, 10f);
            input.Roads[0].EndIntersectionId = 999;

            LogAssert.ignoreFailingMessages = true;
            builder.Build(input, settings, data);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(0, data.RoadCount);
        }

        [Test]
        public void Build_MaxSpeed_IncludesOverrides()
        {
            RoadNetworkBuildInput input = testNetworks.Line(2, 10f);
            input.Roads[1].SpeedOverride = 100f;

            builder.Build(input, settings, data);

            Assert.AreEqual(100f, data.MaxSpeed, 0.001f);
        }

        [Test]
        public void IsDeadEnd_LineEnds_True()
        {
            RoadNetworkBuildInput input = testNetworks.Line(3, 10f);

            builder.Build(input, settings, data);

            Assert.IsTrue(data.IsDeadEnd(0));
            Assert.IsTrue(data.IsDeadEnd(data.IntersectionCount - 1));
            Assert.IsFalse(data.IsDeadEnd(1));
        }
    }
}
