using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadDrawPlannerTests
    {
        private RoadNetworkAuthoring authoring;
        private RoadDrawPlanner planner;
        private List<int> visibleRoadIds;
        private List<bool> detailed;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            planner = new RoadDrawPlanner();
            visibleRoadIds = new List<int>();
            detailed = new List<bool>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        private AuthoringRoad AddRoad(int typeId, Vector3 start, Vector3 end)
        {
            AuthoringRoad road = new AuthoringRoad(authoring.NewRoadId());
            road.SetTypeId(typeId);
            road.Points.Add(start);
            road.Points.Add(end);
            authoring.Roads.Add(road);
            return road;
        }

        private Plane[] BoxFrustum(Bounds bounds)
        {
            return new Plane[]
            {
                new Plane(Vector3.right, bounds.min),
                new Plane(Vector3.left, bounds.max),
                new Plane(Vector3.up, bounds.min),
                new Plane(Vector3.down, bounds.max),
                new Plane(Vector3.forward, bounds.min),
                new Plane(Vector3.back, bounds.max)
            };
        }

        [Test]
        public void Plan_RoadOutsideFrustum_NotVisible()
        {
            AuthoringRoad road = AddRoad(1, new Vector3(1000f, 0f, 1000f), new Vector3(1010f, 0f, 1000f));
            Plane[] frustum = BoxFrustum(new Bounds(new Vector3(5f, 0f, 5f), new Vector3(10f, 10f, 10f)));

            planner.Plan(authoring, 1f, frustum, Vector3.zero, 150f, null, visibleRoadIds, detailed);

            Assert.IsFalse(visibleRoadIds.Contains(road.Id));
        }

        [Test]
        public void Plan_NearRoad_Detailed_FarRoad_NotDetailed()
        {
            AuthoringRoad nearRoad = AddRoad(1, new Vector3(10f, 0f, 0f), new Vector3(20f, 0f, 0f));
            AuthoringRoad farRoad = AddRoad(1, new Vector3(200f, 0f, 0f), new Vector3(210f, 0f, 0f));
            Plane[] frustum = BoxFrustum(new Bounds(new Vector3(110f, 0f, 0f), new Vector3(400f, 20f, 20f)));

            planner.Plan(authoring, 1f, frustum, Vector3.zero, 50f, null, visibleRoadIds, detailed);

            int nearIndex = visibleRoadIds.IndexOf(nearRoad.Id);
            int farIndex = visibleRoadIds.IndexOf(farRoad.Id);

            Assert.GreaterOrEqual(nearIndex, 0);
            Assert.GreaterOrEqual(farIndex, 0);
            Assert.IsTrue(detailed[nearIndex]);
            Assert.IsFalse(detailed[farIndex]);
        }

        [Test]
        public void Plan_TypeFilter_ExcludesOtherTypes()
        {
            AuthoringRoad roadA = AddRoad(1, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AuthoringRoad roadB = AddRoad(2, new Vector3(0f, 0f, 20f), new Vector3(10f, 0f, 20f));
            Plane[] frustum = BoxFrustum(new Bounds(new Vector3(5f, 0f, 10f), new Vector3(40f, 20f, 40f)));

            HashSet<int> typeFilter = new HashSet<int> { 1 };
            planner.Plan(authoring, 1f, frustum, Vector3.zero, 150f, typeFilter, visibleRoadIds, detailed);

            Assert.IsTrue(visibleRoadIds.Contains(roadA.Id));
            Assert.IsFalse(visibleRoadIds.Contains(roadB.Id));
        }

        [Test]
        public void Plan_BoundsRebuiltWhenVersionChanges()
        {
            AuthoringRoad road = AddRoad(1, new Vector3(1000f, 0f, 1000f), new Vector3(1010f, 0f, 1000f));
            Plane[] frustum = BoxFrustum(new Bounds(new Vector3(5f, 0f, 5f), new Vector3(10f, 10f, 10f)));

            planner.Plan(authoring, 1f, frustum, Vector3.zero, 150f, null, visibleRoadIds, detailed);
            Assert.IsFalse(visibleRoadIds.Contains(road.Id));

            road.Points.Clear();
            road.Points.Add(new Vector3(0f, 0f, 0f));
            road.Points.Add(new Vector3(10f, 0f, 0f));
            authoring.MarkChanged();

            planner.Plan(authoring, 1f, frustum, Vector3.zero, 150f, null, visibleRoadIds, detailed);
            Assert.IsTrue(visibleRoadIds.Contains(road.Id));
        }
    }
}
