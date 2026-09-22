using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MarkerLayerTests
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

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            car = CreateCar("TestCar", new Vector3(9000f, 0f, 9000f));

            CreateMap();
            CreateManager();

            view = viewObject.AddComponent<MapView>();
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
        public IEnumerator MarkerInView_Shown()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            GameObject instance = view.MarkerLayer.GetActiveInstance(index);

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.activeSelf);
        }

        [UnityTest]
        public IEnumerator MarkerOutOfView_Hidden()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(5000f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            Assert.IsNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator MarkerLeavesAndReturns_InstanceReused()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            Assert.IsNotNull(view.MarkerLayer.GetActiveInstance(index));
            Assert.AreEqual(1, view.MarkerLayer.transform.childCount);

            marker.transform.position = new Vector3(5000f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.IsNull(view.MarkerLayer.GetActiveInstance(index));
            Assert.AreEqual(1, view.MarkerLayer.transform.childCount);

            marker.transform.position = new Vector3(50f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.IsNotNull(view.MarkerLayer.GetActiveInstance(index));
            Assert.AreEqual(1, view.MarkerLayer.transform.childCount);
        }

        [UnityTest]
        public IEnumerator ChannelNotInView_NotShown()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);
            view.SetChannelMask(MinimapBit);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.Upright, FullMapBit);
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            Assert.IsNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator Upright_NoRotation()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);
            view.SetRotation(45f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            GameObject instance = view.MarkerLayer.GetActiveInstance(index);

            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.localEulerAngles.z), 0.1f);
        }

        [UnityTest]
        public IEnumerator FollowHeading_RotatesWithHeading()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f), MarkerRotationMode.FollowHeading, MinimapBit | FullMapBit);
            marker.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            GameObject instance = view.MarkerLayer.GetActiveInstance(index);
            float rotationA = instance.transform.localEulerAngles.z;

            marker.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            yield return WaitFrames(3);

            float rotationB = instance.transform.localEulerAngles.z;

            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(rotationA, rotationB)), 30f);
        }

        [UnityTest]
        public IEnumerator ConstantSize_ZoomChange_SizeUnchanged()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            GameObject instance = view.MarkerLayer.GetActiveInstance(index);
            Vector3 scaleBefore = instance.transform.localScale;

            view.SetZoomMeters(100f, 999999f);
            yield return WaitFrames(3);

            Assert.AreEqual(scaleBefore, instance.transform.localScale);
        }

        [UnityTest]
        public IEnumerator PlayerOutsideMap_PinnedToEdge()
        {
            MapViewFollowCar followCar = viewObject.AddComponent<MapViewFollowCar>();
            followCar.SetRotationMode(MinimapRotationMode.NorthUp);
            followCar.SetSpeedZoom(false);
            followCar.SetFixedZoomMeters(200f);
            followCar.SetZoomSmoothing(0f);

            car.position = new Vector3(515f, 0f, 0f);
            yield return WaitFrames(3);

            int playerIndex = manager.Markers.PlayerIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            GameObject instance = view.MarkerLayer.GetActiveInstance(playerIndex);
            Assert.IsNotNull(instance);

            RectTransform rect = instance.GetComponent<RectTransform>();
            Assert.AreEqual(92f, rect.anchoredPosition.x, 1f);
            Assert.AreEqual(0f, rect.anchoredPosition.y, 1f);
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

        private MapMarker CreateObjectMarker(Vector3 position)
        {
            return CreateObjectMarker(position, MarkerRotationMode.Upright, MinimapBit | FullMapBit);
        }

        private MapMarker CreateObjectMarker(Vector3 position, MarkerRotationMode rotationMode, int channelMask)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetPrefab(markerTemplate);
            marker.SetRotationMode(rotationMode);
            marker.SetChannelMask(channelMask);
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
            manager.SetPlayerMarkerPrefab(markerTemplate);
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
