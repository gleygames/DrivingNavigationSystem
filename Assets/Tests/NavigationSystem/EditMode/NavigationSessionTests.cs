using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationSessionTests
    {
        private const float Tolerance = 0.001f;

        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RoutePreferences preferences;
        private RouteRequest request;
        private Route route;
        private NavigationSession session;
        private RoadMatcher matcher;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            preferences = new RoutePreferences();
            request = new RouteRequest();
            route = new Route();
            session = new NavigationSession();
        }

        [TearDown]
        public void TearDown()
        {
            if (data != null)
            {
                Object.DestroyImmediate(data);
                data = null;
            }
            matcher = null;
        }

        [Test]
        public void DriveRoute_ProgressIncreases_RemainingDecreases()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 3, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(5f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 5f);
            float previousProgress = session.ProgressDistance;
            float previousRemaining = session.RemainingDistance;

            for (int x = 6; x <= 190; x++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0f), Vector3.right, false, false);
                session.UpdateNavigationSessionLogic(matcher, 1f);

                Assert.Greater(session.ProgressDistance, previousProgress);
                Assert.Less(session.RemainingDistance, previousRemaining);
                previousProgress = session.ProgressDistance;
                previousRemaining = session.RemainingDistance;
            }
        }

        [Test]
        public void NextSegment_Advances()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 3, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(95f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);
            Assert.AreEqual(0, session.CurrentSegment);

            for (int x = 96; x <= 105; x++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0f), Vector3.right, false, false);
                session.UpdateNavigationSessionLogic(matcher, 1f);
            }

            Assert.AreEqual(1, matcher.RoadIndex);
            Assert.AreEqual(1, session.CurrentSegment);
        }

        [Test]
        public void ShortSegmentSkipped_LookAheadAdvances()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(5, 8f));
            pathfinder.FindRouteBetweenIntersections(0, 5, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            for (int x = 1; x <= 20; x++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0f), Vector3.right, false, false);
            }

            Assert.AreEqual(2, matcher.RoadIndex);
            session.UpdateNavigationSessionLogic(matcher, 0f);

            Assert.AreEqual(2, session.CurrentSegment);
            Assert.AreEqual(20f, session.ProgressDistance, Tolerance);
        }

        [Test]
        public void OppositeDirection_AccumulatesWrongWayDistance()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(50f, 0f, 0f), Vector3.left, false, false);
            session.UpdateNavigationSessionLogic(matcher, 5f);
            Assert.AreEqual(5f, session.WrongWayDistance, Tolerance);
            Assert.IsFalse(session.WrongTurn);

            matcher.UpdateRoadMatchingLogic(new Vector3(50f, 0f, 0f), Vector3.left, false, false);
            session.UpdateNavigationSessionLogic(matcher, 5f);
            Assert.AreEqual(10f, session.WrongWayDistance, Tolerance);
        }

        [Test]
        public void UnknownRoad_WrongTurn()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(150f, 0f, 0f), Vector3.right, false, false);
            Assert.AreEqual(1, matcher.RoadIndex);

            session.UpdateNavigationSessionLogic(matcher, 5f);

            Assert.IsTrue(session.WrongTurn);
            Assert.AreEqual(0f, session.WrongWayDistance, Tolerance);
        }

        [Test]
        public void OffRoad_Freezes()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            session.UpdateNavigationSessionLogic(matcher, 5f);

            Assert.IsTrue(session.OffRoad);
            Assert.AreEqual(0f, session.ProgressDistance, Tolerance);
            Assert.AreEqual(route.Length, session.RemainingDistance, Tolerance);
            Assert.AreEqual(route.Eta, session.Eta, Tolerance);
        }

        [Test]
        public void ArrivalPassingPoint_Arrived()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(10f, 0f, 0f), new Vector3(60f, 0f, 0f));
            request.SetHeading(Vector3.right);
            pathfinder.FindRoute(request, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(65f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);

            Assert.IsTrue(session.Arrived);
        }

        [Test]
        public void ArrivalWithinDistance_Arrived()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            request.Set(new Vector3(10f, 0f, 0f), new Vector3(60f, 0f, 0f));
            request.SetHeading(Vector3.right);
            pathfinder.FindRoute(request, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(55f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);

            Assert.Less(matcher.DistanceAlong, 60f);
            Assert.LessOrEqual(session.RemainingDistance, session.ArrivalDistance);
            Assert.IsTrue(session.Arrived);
        }

        [Test]
        public void Eta_UsesRoadSpeeds()
        {
            RoadNetworkBuildInput input = testNetworks.Line(2, 100f);
            input.Roads[0].SpeedOverride = 10f;
            input.Roads[1].SpeedOverride = 20f;
            Pathfinder pathfinder = CreatePathfinder(input);
            pathfinder.FindRouteBetweenIntersections(0, 2, preferences, route);
            session.Start(route);

            Assert.AreEqual(15f, session.Eta, Tolerance);

            matcher = new RoadMatcher(data);
            matcher.UpdateRoadMatchingLogic(new Vector3(50f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 50f);

            Assert.AreEqual(10f, session.Eta, Tolerance);
        }

        [Test]
        public void JumpTo_BackwardOnRoute_ProgressDecreases()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(3, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 3, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            for (int x = 6; x <= 250; x++)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0f), Vector3.right, false, false);
                session.UpdateNavigationSessionLogic(matcher, 1f);
            }

            Assert.AreEqual(2, session.CurrentSegment);
            float previousProgress = session.ProgressDistance;

            matcher.UpdateRoadMatchingLogic(new Vector3(20f, 0f, 0f), Vector3.right, false, true);
            session.JumpTo(matcher);

            Assert.AreEqual(0, session.CurrentSegment);
            Assert.AreEqual(20f, session.ProgressDistance, Tolerance);
            Assert.Less(session.ProgressDistance, previousProgress);
        }

        private Pathfinder CreatePathfinder(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            return new Pathfinder(data);
        }
    }
}
