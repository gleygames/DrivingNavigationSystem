using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewLayerTests
    {
        private const int TestLayer = 5;

        private GameObject rootObject;
        private MapView mapView;

        [SetUp]
        public void SetUp()
        {
            rootObject = new GameObject("MapViewLayerRoot", typeof(RectTransform));
            rootObject.layer = TestLayer;
            LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"));
            TestMapViewHost host = rootObject.AddComponent<TestMapViewHost>();
            mapView = host.View;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(rootObject);
        }

        [Test]
        public void GeneratedObjects_InheritViewportLayer()
        {
            Assert.AreEqual(TestLayer, mapView.BackgroundImage.gameObject.layer);
            Assert.AreEqual(TestLayer, mapView.MapImage.transform.parent.gameObject.layer);
            Assert.AreEqual(TestLayer, mapView.MapImage.gameObject.layer);
            Assert.AreEqual(TestLayer, mapView.ActiveRouteRenderer.gameObject.layer);
            Assert.AreEqual(TestLayer, mapView.PreviewRouteRenderer.gameObject.layer);
            Assert.AreEqual(TestLayer, mapView.MarkerLayer.gameObject.layer);
        }

        [Test]
        public void RouteChunks_InheritRendererLayer()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(20f, 5f) };
            List<float> distances = new List<float> { 0f, 10f, 21.2f };
            List<bool> dashed = new List<bool> { false, false, false };

            mapView.ActiveRouteRenderer.SetLine(points, distances, dashed);

            Transform routeTransform = mapView.ActiveRouteRenderer.transform;
            Assert.Greater(routeTransform.childCount, 0);
            for (int i = 0; i < routeTransform.childCount; i++)
            {
                Assert.AreEqual(TestLayer, routeTransform.GetChild(i).gameObject.layer);
            }
        }
    }
}
