using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadNetworkAuthoringTests
    {
        private RoadNetworkAuthoring authoring;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        [Test]
        public void NewRoadId_NeverReusesAfterRemoval()
        {
            int firstId = authoring.NewRoadId();
            AuthoringRoad road = new AuthoringRoad(firstId);
            authoring.Roads.Add(road);

            authoring.Roads.Remove(road);
            int nextId = authoring.NewRoadId();

            Assert.AreNotEqual(firstId, nextId);
            Assert.AreEqual(firstId + 1, nextId);
        }

        [Test]
        public void MarkChanged_IncrementsVersion()
        {
            int versionBefore = authoring.Version;

            authoring.MarkChanged();

            Assert.AreEqual(versionBefore + 1, authoring.Version);
        }

        [Test]
        public void GetRoadsAtIntersection_ReturnsConnectedRoads()
        {
            int intersectionId = authoring.NewIntersectionId();
            AuthoringRoad connectedByStart = new AuthoringRoad(authoring.NewRoadId());
            connectedByStart.SetStartIntersectionId(intersectionId);
            connectedByStart.SetEndIntersectionId(authoring.NewIntersectionId());
            AuthoringRoad connectedByEnd = new AuthoringRoad(authoring.NewRoadId());
            connectedByEnd.SetStartIntersectionId(authoring.NewIntersectionId());
            connectedByEnd.SetEndIntersectionId(intersectionId);
            AuthoringRoad unrelated = new AuthoringRoad(authoring.NewRoadId());
            unrelated.SetStartIntersectionId(authoring.NewIntersectionId());
            unrelated.SetEndIntersectionId(authoring.NewIntersectionId());
            authoring.Roads.Add(connectedByStart);
            authoring.Roads.Add(connectedByEnd);
            authoring.Roads.Add(unrelated);

            List<AuthoringRoad> result = new List<AuthoringRoad>();
            authoring.GetRoadsAtIntersection(intersectionId, result);

            Assert.AreEqual(2, result.Count);
            Assert.Contains(connectedByStart, result);
            Assert.Contains(connectedByEnd, result);
        }

        [Test]
        public void SetGridCellSize_BumpsVersion()
        {
            int versionBefore = authoring.Version;

            authoring.SetGridCellSize(25f);

            Assert.AreEqual(25f, authoring.GridCellSize, 0.001f);
            Assert.Greater(authoring.Version, versionBefore);
        }
    }
}
