using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RoadQueryTests
    {
        private TestNetworks testNetworks;
        private RoadNetworkData data;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
        }

        [TearDown]
        public void TearDown()
        {
            if (data != null)
            {
                Object.DestroyImmediate(data);
                data = null;
            }
        }

        [Test]
        public void FindNearest_PointOnRoad_DistanceZero()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RoadQuery query = new RoadQuery(data);

            RoadPoint result;
            bool found = query.FindNearest(new Vector3(5f, 0f, 0f), 50f, out result);

            Assert.IsTrue(found);
            Assert.AreEqual(0f, result.Distance, 0.001f);
        }

        [Test]
        public void FindNearest_BeyondMaxDistance_ReturnsFalse()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RoadQuery query = new RoadQuery(data);

            RoadPoint result;
            bool found = query.FindNearest(new Vector3(5f, 0f, 1000f), 50f, out result);

            Assert.IsFalse(found);
        }

        [Test]
        public void FindNearest_BetweenTwoRoads_PicksCloser()
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            BuildIntersection a = new BuildIntersection();
            a.Id = 1;
            a.Position = new Vector3(0f, 0f, 0f);
            BuildIntersection b = new BuildIntersection();
            b.Id = 2;
            b.Position = new Vector3(20f, 0f, 0f);
            BuildIntersection c = new BuildIntersection();
            c.Id = 3;
            c.Position = new Vector3(0f, 0f, 20f);
            BuildIntersection d = new BuildIntersection();
            d.Id = 4;
            d.Position = new Vector3(20f, 0f, 20f);
            input.Intersections.Add(a);
            input.Intersections.Add(b);
            input.Intersections.Add(c);
            input.Intersections.Add(d);

            BuildRoad nearRoad = new BuildRoad();
            nearRoad.Id = 1;
            nearRoad.TypeId = 1;
            nearRoad.StartIntersectionId = 1;
            nearRoad.EndIntersectionId = 2;
            nearRoad.Points.Add(a.Position);
            nearRoad.Points.Add(b.Position);
            input.Roads.Add(nearRoad);

            BuildRoad farRoad = new BuildRoad();
            farRoad.Id = 2;
            farRoad.TypeId = 1;
            farRoad.StartIntersectionId = 3;
            farRoad.EndIntersectionId = 4;
            farRoad.Points.Add(c.Position);
            farRoad.Points.Add(d.Position);
            input.Roads.Add(farRoad);

            data = testNetworks.BuildNetwork(input);
            RoadQuery query = new RoadQuery(data);

            RoadPoint result;
            bool found = query.FindNearest(new Vector3(10f, 0f, 2f), 50f, out result);

            Assert.IsTrue(found);
            Assert.AreEqual(0, result.RoadIndex);
        }

        [Test]
        public void FindNearest_SegmentCrossesManyCells_StillFound()
        {
            RoadNetworkBuildInput input = new RoadNetworkBuildInput();

            BuildIntersection start = new BuildIntersection();
            start.Id = 1;
            start.Position = new Vector3(0f, 0f, 0f);
            BuildIntersection end = new BuildIntersection();
            end.Id = 2;
            end.Position = new Vector3(300f, 0f, 300f);
            input.Intersections.Add(start);
            input.Intersections.Add(end);

            BuildRoad road = new BuildRoad();
            road.Id = 1;
            road.TypeId = 1;
            road.StartIntersectionId = 1;
            road.EndIntersectionId = 2;
            road.Points.Add(start.Position);
            road.Points.Add(end.Position);
            input.Roads.Add(road);

            data = testNetworks.BuildNetwork(input);
            RoadQuery query = new RoadQuery(data);

            RoadPoint result;
            bool found = query.FindNearest(new Vector3(150f, 0f, 150f), 5f, out result);

            Assert.IsTrue(found);
            Assert.AreEqual(0f, result.Distance, 0.001f);
        }

        [Test]
        public void FindNearest_DistanceAlongIsCorrect()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(2, 10f));
            RoadQuery query = new RoadQuery(data);

            RoadPoint result;
            bool found = query.FindNearest(new Vector3(15f, 0f, 0f), 5f, out result);

            Assert.IsTrue(found);
            Assert.AreEqual(1, result.RoadIndex);
            Assert.AreEqual(5f, result.DistanceAlong, 0.001f);
        }

        [Test]
        public void ProjectOnRoad_ClampsToRoadEnds()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RoadQuery query = new RoadQuery(data);

            RoadPoint beforeStart;
            query.ProjectOnRoad(0, new Vector3(-5f, 0f, 0f), out beforeStart);
            Assert.AreEqual(0f, beforeStart.DistanceAlong, 0.001f);

            RoadPoint afterEnd;
            query.ProjectOnRoad(0, new Vector3(20f, 0f, 0f), out afterEnd);
            Assert.AreEqual(10f, afterEnd.DistanceAlong, 0.001f);
        }

        [Test]
        public void Grid_EveryCellEntryReferencesValidSegment()
        {
            data = testNetworks.BuildNetwork(testNetworks.Grid(3, 3, 40f));
            RoadGrid grid = data.Grid;

            int cellTotal = grid.CellsX * grid.CellsZ;
            for (int c = 0; c < cellTotal; c++)
            {
                int start = grid.GetCellStart(c);
                int count = grid.GetCellCount(c);
                for (int e = start; e < start + count; e++)
                {
                    int roadIndex = grid.GetEntryRoad(e);
                    int pointIndex = grid.GetEntryPoint(e);

                    Assert.GreaterOrEqual(roadIndex, 0);
                    Assert.Less(roadIndex, data.RoadCount);

                    RoadRecord road = data.GetRoad(roadIndex);
                    Assert.GreaterOrEqual(pointIndex, road.FirstPoint);
                    Assert.Less(pointIndex, road.FirstPoint + road.PointCount - 1);
                }
            }
        }
    }
}
