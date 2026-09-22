using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewInteractiveTests
    {
        private const float Tolerance = 0.5f;

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
            interactive.SetOpenZoomMeters(150f);

            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
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
        public IEnumerator Open_CenteredOnCar_OpenZoom()
        {
            Assert.AreEqual(150f, interactive.ZoomMeters, Tolerance);
            Assert.AreEqual(manager.CarMapPosition.x, view.CenterMap.x, Tolerance);
            Assert.AreEqual(manager.CarMapPosition.y, view.CenterMap.y, Tolerance);
            Assert.IsTrue(interactive.IsFollowingCar);

            yield break;
        }

        [UnityTest]
        public IEnumerator Pan_StopsFollowing()
        {
            interactive.Pan(new Vector2(20f, 0f));

            Assert.IsFalse(interactive.IsFollowingCar);

            yield break;
        }

        [UnityTest]
        public IEnumerator Zoom_WhileFollowing_KeepsCarCentered()
        {
            interactive.Zoom(2f, Vector2.zero);
            yield return null;

            Assert.AreEqual(75f, interactive.ZoomMeters, Tolerance);
            Assert.IsTrue(interactive.IsFollowingCar);
            Assert.AreEqual(manager.CarMapPosition.x, view.CenterMap.x, Tolerance);
            Assert.AreEqual(manager.CarMapPosition.y, view.CenterMap.y, Tolerance);
        }

        [UnityTest]
        public IEnumerator CenterOnCar_ResumesFollowing()
        {
            interactive.Pan(new Vector2(500f, 0f));
            yield return null;
            Assert.IsFalse(interactive.IsFollowingCar);

            interactive.CenterOnCar();
            yield return null;

            Assert.IsTrue(interactive.IsFollowingCar);
            Assert.AreEqual(manager.CarMapPosition.x, view.CenterMap.x, Tolerance);
            Assert.AreEqual(manager.CarMapPosition.y, view.CenterMap.y, Tolerance);
        }

        [UnityTest]
        public IEnumerator Close_CancelsPreview()
        {
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));
            yield return null;

            Assert.IsTrue(manager.HasPreview);

            interactive.Close();

            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator Pan_CannotLeaveMap()
        {
            interactive.Pan(new Vector2(-100000f, 0f));

            Assert.AreEqual(525f, view.CenterMap.x, 1f);

            yield break;
        }
    }
}
