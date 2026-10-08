using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class MarkerLabelTests
    {
        private const int MinimapBit = 1 << 0;
        private const int FullMapBit = 1 << 1;

        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject markerTemplate;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private Transform car;
        private TestMapViewHost host;
        private MapView view;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            viewObject = new GameObject("MapView", typeof(RectTransform));
            viewObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewRect = viewObject.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.5f, 0.5f);
            viewRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.sizeDelta = new Vector2(200f, 200f);

            markerTemplate = new GameObject("MarkerTemplate", typeof(RectTransform));
            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(markerTemplate.transform, false);
            Text templateText = textObject.AddComponent<Text>();
            MarkerLabel label = markerTemplate.AddComponent<MarkerLabel>();
            label.SetText(templateText);

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            car = CreateCar("TestCar", new Vector3(9000f, 0f, 9000f));

            CreateMap();
            CreateManager();

            host = viewObject.AddComponent<TestMapViewHost>();
            view = host.View;
        }

        [TearDown]
        public void TearDown()
        {
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(markerTemplate);

            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();

            for (int i = 0; i < mapAssets.Count; i++)
            {
                Object.DestroyImmediate(mapAssets[i]);
            }
            mapAssets.Clear();

            if (network != null)
            {
                if (network.Settings != null)
                {
                    Object.DestroyImmediate(network.Settings);
                }
                Object.DestroyImmediate(network);
            }
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator NameSet_ShowsName()
        {
            PrepareView();

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            Text text = GetInstanceText(marker);

            Assert.IsTrue(text.enabled);
            Assert.AreEqual("Acme", text.text);
        }

        [UnityTest]
        public IEnumerator EmptyName_TextDisabled()
        {
            PrepareView();

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            yield return WaitFrames(3);

            Assert.IsFalse(GetInstanceText(marker).enabled);
        }

        [UnityTest]
        public IEnumerator ViewLabelsOff_TextDisabled()
        {
            PrepareView();
            host.Settings.SetShowMarkerLabels(false);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            Assert.IsFalse(GetInstanceText(marker).enabled);
        }

        [UnityTest]
        public IEnumerator ToggleViewLabelsAtRuntime_Rebinds()
        {
            PrepareView();
            host.Settings.SetShowMarkerLabels(false);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));
            Assert.IsFalse(GetInstanceText(marker).enabled);

            host.Settings.SetShowMarkerLabels(true);
            yield return WaitFrames(3);

            Text text = GetInstanceText(marker);
            Assert.AreSame(instance, view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker)));
            Assert.IsTrue(text.enabled);
            Assert.AreEqual("Acme", text.text);
        }

        [UnityTest]
        public IEnumerator Rename_UpdatesText()
        {
            PrepareView();

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            marker.DisplayName = "Globex";
            yield return WaitFrames(3);

            Assert.AreEqual("Globex", GetInstanceText(marker).text);
        }

        [UnityTest]
        public IEnumerator PointMarker_NoLabel()
        {
            PrepareView();
            car.position = new Vector3(60f, 0f, 0f);
            yield return WaitFrames(3);

            int playerIndex = manager.Markers.PlayerIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            GameObject instance = view.MarkerLayer.GetActiveInstance(playerIndex);
            Assert.IsNotNull(instance);
            Assert.IsFalse(instance.GetComponentInChildren<Text>(true).enabled);
        }

        [UnityTest]
        public IEnumerator FollowHeadingMarker_LabelStaysUpright()
        {
            PrepareView();

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.FollowHeading);
            marker.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));
            Text text = instance.GetComponentInChildren<Text>(true);

            Assert.IsTrue(instance.GetComponent<MarkerLabel>().enabled);
            Assert.Greater(Quaternion.Angle(instance.transform.rotation, view.MarkerLayer.transform.rotation), 1f);
            Assert.AreEqual(0f, Quaternion.Angle(text.transform.rotation, view.MarkerLayer.transform.rotation), 0.01f);
        }

        [UnityTest]
        public IEnumerator UprightMarker_LabelComponentDisabled()
        {
            PrepareView();

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright);
            marker.DisplayName = "Acme";
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));

            Assert.IsFalse(instance.GetComponent<MarkerLabel>().enabled);
        }

        private void PrepareView()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);
        }

        private Text GetInstanceText(MapMarker marker)
        {
            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));
            Assert.IsNotNull(instance);
            return instance.GetComponentInChildren<Text>(true);
        }

        private int FindMarkerIndex(MapMarker marker)
        {
            for (int i = 0; i < manager.Markers.EntryCount; i++)
            {
                MarkerEntry entry = manager.Markers.GetEntry(i);
                if (entry.Marker == marker && entry.Alive)
                {
                    return i;
                }
            }
            return -1;
        }

        private MapMarker CreateObjectMarker(Vector3 position, MarkerRotationMode rotationMode)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetPrefab(markerTemplate);
            marker.SetRotationMode(rotationMode);
            marker.SetChannelMask(MinimapBit | FullMapBit);
            createdObjects.Add(markerObject);
            markerObject.SetActive(true);
            return marker;
        }

        private void CreateManager()
        {
            GameObject managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);
            settings.Runtime.SetPlayerMarkerPrefab(markerTemplate);
        }

        private Transform CreateCar(string name, Vector3 position)
        {
            GameObject carObject = new GameObject(name);
            carObject.transform.position = position;
            carObject.transform.rotation = Quaternion.LookRotation(Vector3.right);
            createdObjects.Add(carObject);
            return carObject.transform;
        }

        private void CreateMap()
        {
            Vector3 center = new Vector3(0f, 0f, 0f);

            MapData data = ScriptableObject.CreateInstance<MapData>();
            data.SetRectangleCenter(center);
            data.SetRectangleSize(new Vector2(1000f, 1000f));
            data.SetRectangleRotationY(0f);
            data.SetEditTimeWorldPosition(center);
            data.SetRoadNetwork(network);
            mapAssets.Add(data);

            GameObject mapObject = new GameObject("Map");
            mapObject.SetActive(false);
            mapObject.transform.position = center;
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            map.SetMapData(data);
            createdObjects.Add(mapObject);
            mapObject.SetActive(true);
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
