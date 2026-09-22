using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewRouteTests
    {
        private readonly List<GameObject> maps = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject previewHiddenViewObject;
        private GameObject managerObject;
        private GameObject carObject;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private MapView view;
        private MapView previewHiddenView;
        private MapData mapData;
        private Texture2D mapTexture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            viewObject = CreateViewObject("MapView");
            previewHiddenViewObject = CreateViewObject("PreviewHiddenMapView");

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

            view = viewObject.AddComponent<MapView>();
            previewHiddenView = previewHiddenViewObject.AddComponent<MapView>();
            previewHiddenView.SetShowPreview(false);

            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(managerObject);
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
        public IEnumerator NavigationStarted_ActiveLineBuilt()
        {
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);

            RouteLineGraphic graphic = view.ActiveRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);

            Assert.IsNotNull(graphic);
            Assert.IsTrue(graphic.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Driving_TrimUpdates_NoMeshRebuild()
        {
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);
            RouteLineGraphic graphic = view.ActiveRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);
            int buildCountAfterLine = graphic.MeshBuildCount;

            for (int i = 0; i < 10; i++)
            {
                carObject.transform.position += new Vector3(5f, 0f, 0f);
                yield return WaitAndUpdateCanvases(1);
            }

            Assert.AreEqual(buildCountAfterLine, graphic.MeshBuildCount);
            Assert.Greater(manager.TrimDistance, 0f);
        }

        [UnityTest]
        public IEnumerator Preview_ShownOnlyWhenShowPreview()
        {
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);

            RouteLineGraphic shownGraphic = view.PreviewRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);
            RouteLineGraphic hiddenGraphic = previewHiddenView.PreviewRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);

            Assert.IsNotNull(shownGraphic);
            Assert.IsTrue(shownGraphic.gameObject.activeSelf);
            Assert.IsNull(hiddenGraphic);
        }

        [UnityTest]
        public IEnumerator Stop_ClearsLine()
        {
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitAndUpdateCanvases(2);
            RouteLineGraphic graphic = view.ActiveRouteRenderer.GetComponentInChildren<RouteLineGraphic>(true);
            Assert.IsTrue(graphic.gameObject.activeSelf);

            manager.StopNavigation();
            yield return WaitAndUpdateCanvases(2);

            Assert.IsFalse(graphic.gameObject.activeSelf);
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
