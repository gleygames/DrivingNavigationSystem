using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;
using Gley.NavigationSystem.Dev;

namespace Gley.NavigationSystem.Tests
{
    public class RoadValidatorTests
    {
        private RoadNetworkAuthoring authoring;
        private RoadValidator validator;

        [SetUp]
        public void SetUp()
        {
            authoring = ScriptableObject.CreateInstance<RoadNetworkAuthoring>();
            validator = new RoadValidator();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(authoring);
        }

        private int AddIntersection(Vector3 position)
        {
            int id = authoring.NewIntersectionId();
            authoring.Intersections.Add(new AuthoringIntersection(id, position));
            return id;
        }

        private AuthoringRoad AddRoad(int startIntersectionId, int endIntersectionId, params Vector3[] points)
        {
            AuthoringRoad road = new AuthoringRoad(authoring.NewRoadId());
            road.SetStartIntersectionId(startIntersectionId);
            road.SetEndIntersectionId(endIntersectionId);
            for (int i = 0; i < points.Length; i++)
            {
                road.Points.Add(points[i]);
            }
            authoring.Roads.Add(road);
            return road;
        }

        private bool ContainsKind(List<ValidationIssue> issues, ValidationIssueKind kind)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Kind == kind)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void NearMiss_EndsCloseButNotConnected_Reported()
        {
            int a1 = AddIntersection(new Vector3(0f, 0f, 0f));
            int a2 = AddIntersection(new Vector3(10f, 0f, 0f));
            AddRoad(a1, a2, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));

            int b1 = AddIntersection(new Vector3(10.5f, 0f, 0f));
            int b2 = AddIntersection(new Vector3(20f, 0f, 0f));
            AddRoad(b1, b2, new Vector3(10.5f, 0f, 0f), new Vector3(20f, 0f, 0f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunCheapChecks(authoring, issues);

            Assert.IsTrue(ContainsKind(issues, ValidationIssueKind.NearMiss));
        }

        [Test]
        public void NearMiss_Connected_NotReported()
        {
            int a1 = AddIntersection(new Vector3(0f, 0f, 0f));
            int a2 = AddIntersection(new Vector3(0.5f, 0f, 0f));
            AddRoad(a1, a2, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0f, 0f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunCheapChecks(authoring, issues);

            Assert.IsFalse(ContainsKind(issues, ValidationIssueKind.NearMiss));
        }

        [Test]
        public void Duplicate_SameEndpointsSameShape_Reported()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(5.3f, 0f, 0.2f), new Vector3(10f, 0f, 0f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunCheapChecks(authoring, issues);

            Assert.IsTrue(ContainsKind(issues, ValidationIssueKind.Duplicate));
        }

        [Test]
        public void Duplicate_SameEndpointsDifferentShape_NotReported()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 5f), new Vector3(10f, 0f, 0f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunCheapChecks(authoring, issues);

            Assert.IsFalse(ContainsKind(issues, ValidationIssueKind.Duplicate));
        }

        [Test]
        public void Island_TwoComponents_ReportsSmallerOne()
        {
            int l1 = AddIntersection(new Vector3(0f, 0f, 0f));
            int l2 = AddIntersection(new Vector3(10f, 0f, 0f));
            int l3 = AddIntersection(new Vector3(20f, 0f, 0f));
            int l4 = AddIntersection(new Vector3(30f, 0f, 0f));
            AddRoad(l1, l2, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AddRoad(l2, l3, new Vector3(10f, 0f, 0f), new Vector3(20f, 0f, 0f));
            AddRoad(l3, l4, new Vector3(20f, 0f, 0f), new Vector3(30f, 0f, 0f));

            int s1 = AddIntersection(new Vector3(1000f, 0f, 0f));
            int s2 = AddIntersection(new Vector3(1010f, 0f, 0f));
            AddRoad(s1, s2, new Vector3(1000f, 0f, 0f), new Vector3(1010f, 0f, 0f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(authoring, null, issues);

            int islandCount = 0;
            int reportedIntersectionId = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Kind == ValidationIssueKind.Island)
                {
                    islandCount++;
                    reportedIntersectionId = issues[i].IntersectionId;
                }
            }

            Assert.AreEqual(1, islandCount);
            Assert.AreEqual(s1, reportedIntersectionId);
        }

        [Test]
        public void OneWayTrap_DeadEndOneWay_Reported()
        {
            int l1 = AddIntersection(new Vector3(0f, 0f, 0f));
            int l2 = AddIntersection(new Vector3(10f, 0f, 0f));
            int l3 = AddIntersection(new Vector3(5f, 0f, 10f));
            int t = AddIntersection(new Vector3(0f, 0f, -10f));
            AddRoad(l1, l2, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AddRoad(l2, l3, new Vector3(10f, 0f, 0f), new Vector3(5f, 0f, 10f));
            AddRoad(l3, l1, new Vector3(5f, 0f, 10f), new Vector3(0f, 0f, 0f));
            AuthoringRoad toTrap = AddRoad(l1, t, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -10f));
            toTrap.SetOneWay(true);

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(authoring, null, issues);

            int reportedIntersectionId = 0;
            int trapCount = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Kind == ValidationIssueKind.OneWayTrap)
                {
                    trapCount++;
                    reportedIntersectionId = issues[i].IntersectionId;
                }
            }

            Assert.AreEqual(1, trapCount);
            Assert.AreEqual(t, reportedIntersectionId);
        }

        [Test]
        public void OneWayTrap_OneWayLoop_NotReported()
        {
            int l1 = AddIntersection(new Vector3(0f, 0f, 0f));
            int l2 = AddIntersection(new Vector3(10f, 0f, 0f));
            int l3 = AddIntersection(new Vector3(5f, 0f, 10f));
            AuthoringRoad roadA = AddRoad(l1, l2, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            AuthoringRoad roadB = AddRoad(l2, l3, new Vector3(10f, 0f, 0f), new Vector3(5f, 0f, 10f));
            AuthoringRoad roadC = AddRoad(l3, l1, new Vector3(5f, 0f, 10f), new Vector3(0f, 0f, 0f));
            roadA.SetOneWay(true);
            roadB.SetOneWay(true);
            roadC.SetOneWay(true);

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(authoring, null, issues);

            Assert.IsFalse(ContainsKind(issues, ValidationIssueKind.OneWayTrap));
        }

        [Test]
        public void OutsideMap_RoadBeyondRectangle_Reported()
        {
            MapData map = ScriptableObject.CreateInstance<MapData>();
            map.SetRectangleCenter(new Vector3(50f, 0f, 50f));
            map.SetRectangleSize(new Vector2(100f, 100f));
            map.SetRectangleRotationY(0f);

            int start = AddIntersection(new Vector3(10f, 0f, 10f));
            int end = AddIntersection(new Vector3(150f, 0f, 10f));
            AddRoad(start, end, new Vector3(10f, 0f, 10f), new Vector3(150f, 0f, 10f));

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(authoring, map, issues);

            Assert.IsTrue(ContainsKind(issues, ValidationIssueKind.OutsideMap));

            Object.DestroyImmediate(map);
        }

        [Test]
        public void GroundMiss_Reported()
        {
            int start = AddIntersection(new Vector3(0f, 0f, 0f));
            int end = AddIntersection(new Vector3(10f, 0f, 0f));
            AuthoringRoad road = AddRoad(start, end, new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
            road.GroundMissIndices.Add(1);

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(authoring, null, issues);

            Assert.IsTrue(ContainsKind(issues, ValidationIssueKind.GroundMiss));
        }

        [Test]
        public void FullChecks_LargeGeneratedNetwork_NoStackOverflow()
        {
            TestCityGenerator generator = new TestCityGenerator();
            RoadNetworkBuildInput input = generator.Generate(5000, 1);
            AuthoringFromBuildInput converter = new AuthoringFromBuildInput();
            RoadNetworkAuthoring generated = converter.Convert(input);

            List<ValidationIssue> issues = new List<ValidationIssue>();
            validator.RunFullChecks(generated, null, issues);

            Assert.IsNotNull(issues);

            Object.DestroyImmediate(generated);
        }
    }
}
