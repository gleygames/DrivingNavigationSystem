using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class TapAndPreviewTests
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
        public IEnumerator TapOnMap_PreviewReady()
        {
            bool previewReady = false;
            MapMarker reportedMarker = null;
            manager.PreviewReady += (route, marker) =>
            {
                previewReady = route != null;
                reportedMarker = marker;
            };

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(previewReady);
            Assert.IsNull(reportedMarker);
            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator TapNearDestinationMarker_UsesMarkerPosition_ReportsMarker()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), true);
            yield return WaitFrames(3);

            MapMarker reportedMarker = null;
            manager.PreviewReady += (route, tappedMarker) => reportedMarker = tappedMarker;

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreSame(marker, reportedMarker);
            MarkerEntry entry = manager.Markers.GetEntry(manager.PreviewMarkerIndex);
            Assert.AreEqual(60f, entry.TruePosition.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator TapNearNonDestinationMarker_UsesMapPoint()
        {
            CreateMarker(new Vector3(60f, 0f, 0f), false);
            yield return WaitFrames(3);

            MapMarker reportedMarker = null;
            bool previewReady = false;
            manager.PreviewReady += (route, tappedMarker) =>
            {
                reportedMarker = tappedMarker;
                previewReady = true;
            };

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(previewReady);
            Assert.IsNull(reportedMarker);
        }

        [UnityTest]
        public IEnumerator TwoMarkersInRadius_ClosestWins()
        {
            MapMarker closeMarker = CreateMarker(new Vector3(60f, 0f, 0f), true);
            MapMarker farMarker = CreateMarker(new Vector3(65f, 0f, 0f), true);
            yield return WaitFrames(3);

            MapMarker reportedMarker = null;
            manager.PreviewReady += (route, tappedMarker) => reportedMarker = tappedMarker;

            Vector2 closePoint = ComputeViewportPoint(new Vector3(60f, 0f, 0f));
            Vector2 farPoint = ComputeViewportPoint(new Vector3(65f, 0f, 0f));
            Vector2 tapPoint = Vector2.Lerp(closePoint, farPoint, 0.3f);
            interactive.TapAt(tapPoint);
            yield return null;

            Assert.AreSame(closeMarker, reportedMarker);
            Assert.AreNotSame(farMarker, reportedMarker);
        }

        [UnityTest]
        public IEnumerator ConfirmStepOff_StartsNavigationDirectly()
        {
            interactive.SetConfirmStep(false);

            bool navigationStarted = false;
            manager.NavigationStarted += route => navigationStarted = true;

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(navigationStarted);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator PreviewPanel_ShowsOnReady_HidesOnCancel()
        {
            GameObject panelRoot = CreateChild(viewObject, "Panel");
            panelRoot.SetActive(false);
            PreviewPanel panel = CreateDisabledComponent<PreviewPanel>(viewObject, "PreviewPanel");
            panel.SetManager(manager);
            panel.SetPanelRoot(panelRoot);
            panel.gameObject.SetActive(true);

            manager.PreviewDestination(new Vector3(60f, 0f, 0f));
            yield return null;

            Assert.IsTrue(panelRoot.activeSelf);

            manager.CancelPreview();
            yield return null;

            Assert.IsFalse(panelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator ConfirmButton_StartsNavigation()
        {
            GameObject panelRoot = CreateChild(viewObject, "Panel");
            GameObject confirmObject = CreateChild(panelRoot, "Confirm");
            Button confirmButton = confirmObject.AddComponent<Button>();

            PreviewPanel panel = CreateDisabledComponent<PreviewPanel>(viewObject, "PreviewPanel");
            panel.SetManager(manager);
            panel.SetPanelRoot(panelRoot);
            panel.SetConfirmButton(confirmButton);
            panel.gameObject.SetActive(true);

            manager.PreviewDestination(new Vector3(60f, 0f, 0f));
            yield return null;

            confirmButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator StopButton_VisibleOnlyWithActiveRoute()
        {
            GameObject stopObject = CreateChild(viewObject, "Stop");
            Button stopButton = stopObject.AddComponent<Button>();

            NavigationControls controls = CreateDisabledComponent<NavigationControls>(viewObject, "Controls");
            controls.SetManager(manager);
            controls.SetInteractive(interactive);
            controls.SetStopButton(stopButton);
            controls.gameObject.SetActive(true);
            yield return null;

            Assert.IsFalse(stopObject.activeSelf);

            manager.StartNavigation(new Vector3(60f, 0f, 0f));
            yield return null;

            Assert.IsTrue(stopObject.activeSelf);

            manager.StopNavigation();
            yield return null;

            Assert.IsFalse(stopObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator CenterButton_VisibleOnlyWhenNotFollowing()
        {
            GameObject centerObject = CreateChild(viewObject, "Center");
            Button centerButton = centerObject.AddComponent<Button>();

            NavigationControls controls = CreateDisabledComponent<NavigationControls>(viewObject, "Controls");
            controls.SetManager(manager);
            controls.SetInteractive(interactive);
            controls.SetCenterButton(centerButton);
            controls.gameObject.SetActive(true);
            yield return null;

            Assert.IsFalse(centerObject.activeSelf);

            interactive.Pan(new Vector2(20f, 0f));
            yield return null;

            Assert.IsTrue(centerObject.activeSelf);

            interactive.CenterOnCar();
            yield return null;

            Assert.IsFalse(centerObject.activeSelf);
        }

        private Vector2 ComputeViewportPoint(Vector3 truePosition)
        {
            Vector2 mapPoint = mapData.CreateFrame().TrueToMap(truePosition);
            Vector2 delta = mapPoint - view.CenterMap;
            return delta * view.CanvasUnitsPerMeter;
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

        private GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private T CreateDisabledComponent<T>(GameObject parent, string name) where T : Component
        {
            GameObject host = CreateChild(parent, name);
            host.SetActive(false);
            return host.AddComponent<T>();
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
