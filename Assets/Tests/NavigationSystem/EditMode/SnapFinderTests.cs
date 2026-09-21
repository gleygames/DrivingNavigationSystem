using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class SnapFinderTests
    {
        private RoadNetworkAuthoring authoring;
        private RoadEditOperations ops;
        private SnapFinder finder;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            ops = new RoadEditOperations(authoring, new FlatGroundProbe(0f), 0.1f, 20f);
            finder = new SnapFinder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        private int CreateStraightRoad(Vector3 start, Vector3 end)
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(start);
            points.Add(end);
            return ops.CreateRoad(points, new RoadBrush(), 0, 0);
        }

        [Test]
        public void FindSnap_IntersectionWithinDistance_PrefersIntersection()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f));
            AuthoringRoad road = authoring.FindRoad(roadId);

            SnapTarget target;
            bool found = finder.FindSnap(new Vector3(1f, 0f, 1f), 3f, authoring, 0, out target);

            Assert.IsTrue(found);
            Assert.AreEqual(SnapTargetKind.Intersection, target.Kind);
            Assert.AreEqual(road.StartIntersectionId, target.IntersectionId);
            Assert.AreEqual(0f, target.Position.x, 0.001f);
            Assert.AreEqual(0f, target.Position.z, 0.001f);
        }

        [Test]
        public void FindSnap_RoadMiddle_ReturnsSegmentAndT()
        {
            int roadId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f));

            SnapTarget target;
            bool found = finder.FindSnap(new Vector3(50f, 0f, 1f), 3f, authoring, 0, out target);

            Assert.IsTrue(found);
            Assert.AreEqual(SnapTargetKind.Road, target.Kind);
            Assert.AreEqual(roadId, target.RoadId);
            Assert.AreEqual(0, target.Segment);
            Assert.AreEqual(0.5f, target.T, 0.001f);
            Assert.AreEqual(50f, target.Position.x, 0.01f);
            Assert.AreEqual(0f, target.Position.z, 0.01f);
        }

        [Test]
        public void FindSnap_NothingClose_ReturnsFalse()
        {
            CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f));

            SnapTarget target;
            bool found = finder.FindSnap(new Vector3(50f, 0f, 10f), 3f, authoring, 0, out target);

            Assert.IsFalse(found);
            Assert.AreEqual(SnapTargetKind.None, target.Kind);
        }

        [Test]
        public void FindSnap_IgnoresGivenRoad()
        {
            int ignoredId = CreateStraightRoad(new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f));
            int otherId = CreateStraightRoad(new Vector3(0f, 0f, 2f), new Vector3(100f, 0f, 2f));

            SnapTarget target;
            bool found = finder.FindSnap(new Vector3(50f, 0f, 0.5f), 3f, authoring, ignoredId, out target);

            Assert.IsTrue(found);
            Assert.AreEqual(SnapTargetKind.Road, target.Kind);
            Assert.AreEqual(otherId, target.RoadId);

            bool foundOnlyIgnored = finder.FindSnap(new Vector3(50f, 0f, -1f), 1.5f, authoring, ignoredId, out target);

            Assert.IsFalse(foundOnlyIgnored);
        }
    }
}
