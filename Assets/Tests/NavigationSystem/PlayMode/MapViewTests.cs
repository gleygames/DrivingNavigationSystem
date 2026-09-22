using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewTests
    {
        private const float Tolerance = 0.5f;

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

            viewObject = new GameObject("MapView", typeof(RectTransform));
            viewObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewRect = viewObject.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;

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
            mapData.SetOutsideMapColor(new Color(0.3f, 0.4f, 0.5f, 1f));
            mapAssets.Add(mapData);

            GameObject mapObject = new GameObject("Map");
            mapObject.SetActive(false);
            mapObject.transform.position = center;
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            map.SetMapData(mapData);
            maps.Add(mapObject);
            mapObject.SetActive(true);

            view = viewObject.AddComponent<MapView>();

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
        public IEnumerator Background_IsRaycastTarget_OthersNot()
        {
            Assert.IsTrue(view.BackgroundImage.raycastTarget);
            Assert.IsFalse(view.MapImage.raycastTarget);
            yield break;
        }

        [UnityTest]
        public IEnumerator MapChanged_ImageAssigned_SizeMatchesRectangle()
        {
            Assert.AreSame(mapTexture, view.MapImage.texture);
            Assert.AreEqual(mapData.RectangleSize, view.MapImage.rectTransform.sizeDelta);
            yield break;
        }

        [UnityTest]
        public IEnumerator SetZoomMeters_ClampsToMin()
        {
            view.SetZoomMeters(1f, 1000f);

            Assert.AreEqual(50f, view.ZoomMeters, Tolerance);
            yield break;
        }

        [UnityTest]
        public IEnumerator ScreenToWorld_WorldToScreen_RoundTrip()
        {
            Vector3 worldPoint = new Vector3(160f, 0f, 10f);

            Vector2 screenPoint = view.WorldToScreen(worldPoint);
            Vector3 roundTrip = view.ScreenToWorld(screenPoint);

            Assert.AreEqual(worldPoint.x, roundTrip.x, Tolerance);
            Assert.AreEqual(worldPoint.z, roundTrip.z, Tolerance);
            yield break;
        }

        [UnityTest]
        public IEnumerator Background_UsesOutsideMapColor()
        {
            Assert.AreEqual(mapData.OutsideMapColor, view.BackgroundImage.color);
            yield break;
        }
    }
}
