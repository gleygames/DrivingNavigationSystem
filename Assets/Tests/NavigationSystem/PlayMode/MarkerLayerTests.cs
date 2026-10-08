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
        public IEnumerator EmptyPrefab_UsesSettingsDefaultMarker()
        {
            settings.Runtime.SetDefaultMarkerPrefab(markerTemplate);
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarkerWithoutPrefab(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            Assert.IsNotNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator EmptyPrefab_NoDefault_ShowsNothing()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarkerWithoutPrefab(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            int index = FindMarkerIndex(marker);
            Assert.IsNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator MarkerEntersView_BoundToMarkerAndView()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));
            FakeMarkerVisual visual = instance.GetComponent<FakeMarkerVisual>();

            Assert.AreEqual(1, visual.BindCount);
            Assert.AreSame(marker, visual.BoundMarker);
            Assert.AreSame(view, visual.BoundView);
        }

        [UnityTest]
        public IEnumerator MarkerLeavesView_Unbound()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker));
            FakeMarkerVisual visual = instance.GetComponent<FakeMarkerVisual>();

            marker.transform.position = new Vector3(5000f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.AreEqual(1, visual.UnbindCount);
            Assert.IsFalse(visual.IsBound);
        }

        [UnityTest]
        public IEnumerator PooledInstanceReused_RebindsToNewMarker()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker markerA = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerA));
            FakeMarkerVisual visual = instance.GetComponent<FakeMarkerVisual>();

            markerA.transform.position = new Vector3(5000f, 0f, 0f);
            yield return WaitFrames(3);

            MapMarker markerB = CreateObjectMarker(new Vector3(60f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instanceB = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerB));
            Assert.AreSame(instance, instanceB);
            Assert.AreEqual(2, visual.BindCount);
            Assert.AreSame(markerB, visual.BoundMarker);
        }

        [UnityTest]
        public IEnumerator IndexReusedSameFrame_NewInstanceForNewMarker()
        {
            GameObject templateB = new GameObject("MarkerTemplateB", typeof(RectTransform));
            createdObjects.Add(templateB);
            markerTemplate.AddComponent<FakeMarkerVisual>();
            templateB.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker markerA = CreateObjectMarkerWithPrefab(new Vector3(50f, 0f, 0f), markerTemplate);
            yield return WaitFrames(3);

            int indexA = FindMarkerIndex(markerA);
            GameObject instanceA = view.MarkerLayer.GetActiveInstance(indexA);
            FakeMarkerVisual visualA = instanceA.GetComponent<FakeMarkerVisual>();

            markerA.gameObject.SetActive(false);
            MapMarker markerB = CreateObjectMarkerWithPrefab(new Vector3(50f, 0f, 0f), templateB);
            int indexB = FindMarkerIndex(markerB);
            Assert.AreEqual(indexA, indexB);

            yield return WaitFrames(3);

            GameObject instanceB = view.MarkerLayer.GetActiveInstance(indexB);
            Assert.IsNotNull(instanceB);
            Assert.AreNotSame(instanceA, instanceB);
            Assert.AreSame(markerB, instanceB.GetComponent<FakeMarkerVisual>().BoundMarker);
            Assert.AreEqual(1, visualA.UnbindCount);
        }

        [UnityTest]
        public IEnumerator PointMarker_BoundWithNullMarker()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            settings.Runtime.SetDestinationMarkerPrefab(markerTemplate);
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);
            yield return WaitFrames(2);

            car.position = new Vector3(10f, 0f, 0f);
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(60f, 0f, 0f));
            yield return WaitFrames(3);

            int index = manager.DestinationMarkerIndex;
            Assert.GreaterOrEqual(index, 0);

            GameObject instance = view.MarkerLayer.GetActiveInstance(index);
            Assert.IsNotNull(instance);

            FakeMarkerVisual visual = instance.GetComponent<FakeMarkerVisual>();
            Assert.AreEqual(1, visual.BindCount);
            Assert.IsNull(visual.BoundMarker);
            Assert.IsTrue(visual.IsBound);
        }

        [UnityTest]
        public IEnumerator SelectMarker_VisualSelected()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            FakeMarkerVisual visual = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker)).GetComponent<FakeMarkerVisual>();
            Assert.IsFalse(visual.IsSelected);

            manager.SelectMarker(marker);
            yield return WaitFrames(1);

            Assert.IsTrue(visual.IsSelected);
        }

        [UnityTest]
        public IEnumerator ClearSelection_VisualUnselected()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker marker = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            FakeMarkerVisual visual = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(marker)).GetComponent<FakeMarkerVisual>();
            manager.SelectMarker(marker);
            yield return WaitFrames(2);
            Assert.IsTrue(visual.IsSelected);

            manager.ClearSelection();
            yield return WaitFrames(2);

            Assert.IsFalse(visual.IsSelected);
        }

        [UnityTest]
        public IEnumerator SelectOther_OldUnselectedNewSelected()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker markerA = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            MapMarker markerB = CreateObjectMarker(new Vector3(70f, 0f, 0f));
            yield return WaitFrames(3);

            FakeMarkerVisual visualA = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerA)).GetComponent<FakeMarkerVisual>();
            FakeMarkerVisual visualB = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerB)).GetComponent<FakeMarkerVisual>();

            manager.SelectMarker(markerA);
            yield return WaitFrames(2);
            Assert.IsTrue(visualA.IsSelected);
            Assert.IsFalse(visualB.IsSelected);

            manager.SelectMarker(markerB);
            yield return WaitFrames(2);

            Assert.IsFalse(visualA.IsSelected);
            Assert.IsTrue(visualB.IsSelected);
        }

        [UnityTest]
        public IEnumerator SelectedMarker_IsLastSibling()
        {
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker markerA = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            MapMarker markerB = CreateObjectMarker(new Vector3(60f, 0f, 0f));
            MapMarker markerC = CreateObjectMarker(new Vector3(70f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instanceA = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerA));
            GameObject instanceB = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerB));
            GameObject instanceC = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerC));

            manager.SelectMarker(markerA);
            yield return WaitFrames(3);

            int siblingA = instanceA.transform.GetSiblingIndex();
            Assert.Greater(siblingA, instanceB.transform.GetSiblingIndex());
            Assert.Greater(siblingA, instanceC.transform.GetSiblingIndex());
        }

        [UnityTest]
        public IEnumerator ReusedInstance_StartsUnselected()
        {
            markerTemplate.AddComponent<FakeMarkerVisual>();
            view.SetCenter(new Vector2(500f, 500f));
            view.SetZoomMeters(200f, 999999f);

            MapMarker markerA = CreateObjectMarker(new Vector3(50f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instance = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerA));
            FakeMarkerVisual visual = instance.GetComponent<FakeMarkerVisual>();

            manager.SelectMarker(markerA);
            yield return WaitFrames(2);
            Assert.IsTrue(visual.IsSelected);

            markerA.transform.position = new Vector3(5000f, 0f, 0f);
            yield return WaitFrames(3);

            MapMarker markerB = CreateObjectMarker(new Vector3(60f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject instanceB = view.MarkerLayer.GetActiveInstance(FindMarkerIndex(markerB));
            Assert.AreSame(instance, instanceB);
            Assert.IsFalse(visual.IsSelected);
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
            host.Settings.SetChannelMask(MinimapBit);

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
            MinimapTestRig rig = CreateMinimapRig();
            rig.Activate();

            car.position = new Vector3(515f, 0f, 0f);
            yield return WaitFrames(3);

            int playerIndex = manager.Markers.PlayerIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            GameObject instance = rig.View.MarkerLayer.GetActiveInstance(playerIndex);
            Assert.IsNotNull(instance);

            RectTransform rect = instance.GetComponent<RectTransform>();
            Assert.AreEqual(92f, rect.anchoredPosition.x, 1f);
            Assert.AreEqual(0f, rect.anchoredPosition.y, 1f);
        }

        [UnityTest]
        public IEnumerator ViewportPivotBottomLeft_PlayerAtCarPosition()
        {
            MinimapTestRig rig = CreateMinimapRig();
            rig.Viewport.pivot = Vector2.zero;
            rig.Activate();

            car.position = Vector3.zero;
            yield return WaitFrames(3);

            int playerIndex = manager.Markers.PlayerIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            GameObject instance = rig.View.MarkerLayer.GetActiveInstance(playerIndex);
            Assert.IsNotNull(instance);

            RectTransform rect = instance.GetComponent<RectTransform>();
            Assert.AreEqual(0f, rect.anchoredPosition.x, 1f);
            Assert.AreEqual(0f, rect.anchoredPosition.y, 1f);
        }

        private MinimapTestRig CreateMinimapRig()
        {
            MinimapTestRig rig = new MinimapTestRig(canvasObject.transform);
            RectTransform rootRect = rig.RootRect;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(200f, 200f);

            MinimapFollowSettings followSettings = rig.Minimap.FollowSettings;
            followSettings.SetRotationMode(MinimapRotationMode.NorthUp);
            followSettings.SetSpeedZoom(false);
            followSettings.SetFixedZoomMeters(200f);
            followSettings.SetZoomSmoothing(0f);
            return rig;
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

        private MapMarker CreateObjectMarkerWithPrefab(Vector3 position, GameObject prefab)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetPrefab(prefab);
            marker.SetRotationMode(MarkerRotationMode.Upright);
            marker.SetChannelMask(MinimapBit | FullMapBit);
            createdObjects.Add(markerObject);
            markerObject.SetActive(true);
            return marker;
        }

        private MapMarker CreateObjectMarkerWithoutPrefab(Vector3 position)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetPrefab(null);
            marker.SetRotationMode(MarkerRotationMode.Upright);
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
