using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class CrosshairTests
    {
        private const float Tolerance = 1f;

        private readonly List<GameObject> createdObjects = new List<GameObject>();

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject managerObject;
        private GameObject carObject;
        private GameObject mapObject;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private MapView view;
        private MapViewInteractive interactive;
        private MapData mapData;
        private Texture2D mapTexture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            viewObject = new GameObject("FullMap", typeof(RectTransform));
            viewObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewRect = viewObject.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.5f, 0.5f);
            viewRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.sizeDelta = new Vector2(400f, 400f);

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            carObject = new GameObject("TestCar");
            carObject.transform.position = new Vector3(20f, 0f, 0f);
            carObject.transform.rotation = Quaternion.LookRotation(Vector3.right);

            managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(carObject.transform, 0f);
            manager.SetStartManually(false);

            mapTexture = new Texture2D(4, 4);

            Vector3 center = new Vector3(150f, 0f, 0f);
            mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.SetRectangleCenter(center);
            mapData.SetRectangleSize(new Vector2(600f, 400f));
            mapData.SetRectangleRotationY(0f);
            mapData.SetEditTimeWorldPosition(center);
            mapData.SetRoadNetwork(network);
            mapData.SetImage(mapTexture);

            mapObject = new GameObject("Map");
            mapObject.SetActive(false);
            mapObject.transform.position = center;
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            map.SetMapData(mapData);
            mapObject.SetActive(true);

            view = viewObject.AddComponent<MapView>();
            interactive = viewObject.AddComponent<MapViewInteractive>();
            interactive.SetOpenZoomMeters(200f);

            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();

            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(carObject);
            Object.DestroyImmediate(mapObject);
            Object.DestroyImmediate(mapData);
            Object.DestroyImmediate(mapTexture);
            if (network.Settings != null)
            {
                Object.DestroyImmediate(network.Settings);
            }
            Object.DestroyImmediate(network);
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator ConfirmAtCrosshair_PreviewsAtCenter()
        {
            Route previewedRoute = null;
            manager.PreviewReady += (route, marker) => previewedRoute = route;

            interactive.ConfirmAtCrosshair();
            yield return null;

            Assert.IsNotNull(previewedRoute);
            Assert.AreEqual(20f, previewedRoute.Destination.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator ConfirmAtCrosshair_NearDestinationMarker_SnapsToMarker()
        {
            MapMarker marker = CreateMarker(new Vector3(22f, 0f, 0f), true);
            yield return WaitFrames(3);

            MapMarker reportedMarker = null;
            manager.PreviewReady += (route, tappedMarker) => reportedMarker = tappedMarker;

            interactive.ConfirmAtCrosshair();
            yield return null;

            Assert.AreSame(marker, reportedMarker);
        }

        [UnityTest]
        public IEnumerator PanByStick_FullTilt_HalfViewPerSecond()
        {
            Vector2 centerBefore = view.CenterMap;

            interactive.PanByStick(Vector2.left, 1f);

            Vector2 centerAfter = view.CenterMap;
            Assert.AreEqual(centerBefore.x + 100f, centerAfter.x, Tolerance);
            Assert.AreEqual(centerBefore.y, centerAfter.y, Tolerance);
            Assert.IsFalse(interactive.IsFollowingCar);

            yield return null;
        }

        private MapMarker CreateMarker(Vector3 position, bool canBeDestination)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetCanBeDestination(canBeDestination);
            createdObjects.Add(markerObject);
            markerObject.SetActive(true);
            return marker;
        }

        private IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
