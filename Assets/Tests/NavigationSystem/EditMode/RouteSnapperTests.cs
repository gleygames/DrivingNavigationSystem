using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RouteSnapperTests
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
        public void SnapStart_Within200m_Succeeds()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RouteSnapper snapper = new RouteSnapper(data);
            RouteRequest request = new RouteRequest();
            request.Set(new Vector3(5f, 0f, 150f), new Vector3(5f, 0f, 0f));

            RoadPoint point;
            FailureReason failure = snapper.SnapStart(request, out point);

            Assert.AreEqual(FailureReason.None, failure);
        }

        [Test]
        public void SnapStart_Beyond200m_NoRoadNearStart()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RouteSnapper snapper = new RouteSnapper(data);
            RouteRequest request = new RouteRequest();
            request.Set(new Vector3(5f, 0f, 250f), new Vector3(5f, 0f, 0f));

            RoadPoint point;
            FailureReason failure = snapper.SnapStart(request, out point);

            Assert.AreEqual(FailureReason.NoRoadNearStart, failure);
        }

        [Test]
        public void SnapDestination_Beyond50m_NoRoadNearDestination()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RouteSnapper snapper = new RouteSnapper(data);
            RouteRequest request = new RouteRequest();
            request.Set(new Vector3(5f, 0f, 0f), new Vector3(5f, 0f, 60f));

            RoadPoint point;
            FailureReason failure = snapper.SnapDestination(request, out point);

            Assert.AreEqual(FailureReason.NoRoadNearDestination, failure);
        }

        [Test]
        public void SnapStart_UsesStartDistance_NotDestinationDistance()
        {
            data = testNetworks.BuildNetwork(testNetworks.Line(1, 10f));
            RouteSnapper snapper = new RouteSnapper(data);
            RouteRequest request = new RouteRequest();
            request.Set(new Vector3(5f, 0f, 120f), new Vector3(5f, 0f, 120f));

            RoadPoint startPoint;
            FailureReason startFailure = snapper.SnapStart(request, out startPoint);

            RoadPoint destinationPoint;
            FailureReason destinationFailure = snapper.SnapDestination(request, out destinationPoint);

            Assert.AreEqual(FailureReason.None, startFailure);
            Assert.AreEqual(FailureReason.NoRoadNearDestination, destinationFailure);
        }
    }
}
