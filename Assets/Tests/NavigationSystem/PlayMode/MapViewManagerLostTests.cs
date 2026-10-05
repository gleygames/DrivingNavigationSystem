using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewManagerLostTests
    {
        private readonly List<GameObject> maps = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject managerObject;
        private GameObject carObject;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private MapView view;
        private MapData mapData;
        private Texture2D mapTexture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            viewObject = CreateViewObject("MapView");

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
            mapData.SetRectangleSize(new Vector2(400f, 200f));
            mapData.SetRectangleRotationY(0f);
            mapData.SetEditTimeWorldPosition(center);
            mapData.SetRoadNetwork(network);
            mapData.SetImage(mapTexture);
            mapAssets.Add(mapData);

            GameObject mapObject = new GameObject("Map");
            mapObject.SetActive(false);
            mapObject.transform.position = center;
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            map.SetMapData(mapData);
            maps.Add(mapObject);
            mapObject.SetActive(true);

            TestMapViewHost host = viewObject.AddComponent<TestMapViewHost>();
            view = host.View;

            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
            if (managerObject != null)
            {
                Object.DestroyImmediate(managerObject);
            }
            Object.DestroyImmediate(carObject);
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    Object.DestroyImmediate(maps[i]);
                }
            }
            maps.Clear();
            for (int i = 0; i < mapAssets.Count; i++)
            {
                Object.DestroyImmediate(mapAssets[i]);
            }
            mapAssets.Clear();
            Object.DestroyImmediate(mapTexture);
            if (network.Settings != null)
            {
                Object.DestroyImmediate(network.Settings);
            }
            Object.DestroyImmediate(network);
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator ManagerDestroyed_LogsErrorOnce_ManagerNull()
        {
            Assert.IsNotNull(view.Manager);
            LogAssert.Expect(LogType.Error, new Regex("MapView on '.*': the NavigationManager was destroyed"));

            Object.Destroy(managerObject);
            yield return WaitAndUpdateCanvases(3);

            Assert.IsNull(view.Manager);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ManagerDestroyed_DuringNavigation_ActiveLineCleared()
        {
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);
            RouteLineGraphic graphic = view.ActiveRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);
            Assert.IsNotNull(graphic);
            Assert.IsTrue(graphic.gameObject.activeSelf);
            LogAssert.Expect(LogType.Error, new Regex("MapView on '.*': the NavigationManager was destroyed"));

            Object.Destroy(managerObject);
            yield return WaitAndUpdateCanvases(2);

            Assert.IsFalse(graphic.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ViewInstantiatedAfterNavigationStarted_ShowsActiveLine()
        {
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);

            GameObject lateViewObject = CreateViewObject("LateMapView");
            TestMapViewHost lateHost = lateViewObject.AddComponent<TestMapViewHost>();
            MapView lateView = lateHost.View;
            yield return WaitAndUpdateCanvases(2);

            RouteLineGraphic graphic = lateView.ActiveRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);

            Assert.IsNotNull(graphic);
            Assert.IsTrue(graphic.gameObject.activeSelf);
        }

        private GameObject CreateViewObject(string name)
        {
            GameObject viewGameObject = new GameObject(name, typeof(RectTransform));
            viewGameObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewRect = viewGameObject.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            return viewGameObject;
        }

        private IEnumerator WaitAndUpdateCanvases(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
