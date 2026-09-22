using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RerouteDeciderTests
    {
        private TestNetworks testNetworks;
        private RoadNetworkData data;
        private RoutePreferences preferences;
        private Route route;
        private NavigationSession session;
        private RoadMatcher matcher;
        private RerouteDecider decider;

        [SetUp]
        public void SetUp()
        {
            testNetworks = new TestNetworks();
            preferences = new RoutePreferences();
            route = new Route();
            session = new NavigationSession();
            decider = new RerouteDecider();
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
        public void WrongTurn_AfterCooldown_Reroutes()
        {
            BuildWrongTurnRoute();
            decider.AccumulateDrivenDistance(decider.RerouteCooldown);

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.WrongTurn, reason);
        }

        [Test]
        public void WrongTurn_DuringCooldown_Waits()
        {
            BuildWrongTurnRoute();

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.None, reason);
        }

        [Test]
        public void TurnedAround_Under30m_NoReroute()
        {
            BuildOppositeDirectionRoute(20f);
            decider.AccumulateDrivenDistance(decider.RerouteCooldown);

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.None, reason);
        }

        [Test]
        public void TurnedAround_30m_Reroutes()
        {
            BuildOppositeDirectionRoute(30f);
            decider.AccumulateDrivenDistance(decider.RerouteCooldown);

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.TurnedAround, reason);
        }

        [Test]
        public void OffRoadBackOnRouteRoad_NoReroute()
        {
            BuildBackOnRoadScenario(new Vector3(50f, 0f, 0f));
            decider.AccumulateDrivenDistance(decider.RerouteCooldown);

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.None, reason);
        }

        [Test]
        public void OffRoadBackOnOtherRoad_BackOnRoad()
        {
            BuildBackOnRoadScenario(new Vector3(150f, 0f, 0f));
            decider.AccumulateDrivenDistance(decider.RerouteCooldown);

            RerouteReason reason = decider.Decide(session, matcher, false);

            Assert.AreEqual(RerouteReason.BackOnRoad, reason);
        }

        [Test]
        public void Teleport_OffRoute_IgnoresCooldown()
        {
            BuildTeleportRoute();

            matcher.UpdateRoadMatchingLogic(new Vector3(150f, 0f, 0f), Vector3.right, false, true);
            RerouteReason reason = decider.Decide(session, matcher, true);

            Assert.AreEqual(RerouteReason.Teleported, reason);
        }

        [Test]
        public void Teleport_OnRoute_None()
        {
            BuildTeleportRoute();

            matcher.UpdateRoadMatchingLogic(new Vector3(50f, 0f, 0f), Vector3.right, false, true);
            RerouteReason reason = decider.Decide(session, matcher, true);

            Assert.AreEqual(RerouteReason.None, reason);
        }

        private void BuildWrongTurnRoute()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            for (int x = 10; x <= 150; x += 10)
            {
                matcher.UpdateRoadMatchingLogic(new Vector3(x, 0f, 0f), Vector3.right, false, false);
                session.UpdateNavigationSessionLogic(matcher, 10f);
            }
        }

        private void BuildOppositeDirectionRoute(float wrongWayDistance)
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(1, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(40f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);

            matcher.UpdateRoadMatchingLogic(new Vector3(40f, 0f, 0f), Vector3.left, false, false);
            session.UpdateNavigationSessionLogic(matcher, wrongWayDistance);
        }

        private void BuildBackOnRoadScenario(Vector3 reEntryPosition)
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);

            matcher.UpdateRoadMatchingLogic(new Vector3(40f, 0f, 0f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);

            matcher.UpdateRoadMatchingLogic(new Vector3(40f, 0f, 500f), Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);
            Assert.IsFalse(matcher.IsOnRoad);

            matcher.UpdateRoadMatchingLogic(reEntryPosition, Vector3.right, false, false);
            session.UpdateNavigationSessionLogic(matcher, 0f);
            Assert.IsTrue(matcher.EnteredRoad);
        }

        private void BuildTeleportRoute()
        {
            Pathfinder pathfinder = CreatePathfinder(testNetworks.Line(2, 100f));
            pathfinder.FindRouteBetweenIntersections(0, 1, preferences, route);
            session.Start(route);
            matcher = new RoadMatcher(data);
        }

        private Pathfinder CreatePathfinder(RoadNetworkBuildInput input)
        {
            data = testNetworks.BuildNetwork(input);
            return new Pathfinder(data);
        }
    }
}
