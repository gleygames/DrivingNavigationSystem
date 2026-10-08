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
        private readonly List<string> events = new List<string>();

        private GameObject canvasObject;
        private FullMapTestRig rig;
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
        private MapMarker reportedMarker;
        private bool previewReadyRaised;
        private bool previewFailedRaised;
        private bool navigationStartedRaised;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            events.Clear();
            reportedMarker = null;
            previewReadyRaised = false;
            previewFailedRaised = false;
            navigationStartedRaised = false;

            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            rig = new FullMapTestRig(canvasObject.transform);
            RectTransform viewRect = rig.RootRect;
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

            rig.FullMap.InteractionSettings.SetOpenZoomMeters(200f);
            rig.Activate();
            view = rig.View;
            interactive = rig.Interactive;

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
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
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
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.None);
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
            MapMarker closeMarker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            MapMarker farMarker = CreateMarker(new Vector3(65f, 0f, 0f), MarkerTapAction.Destination);
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
            rig.FullMap.InteractionSettings.SetConfirmStep(false);

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
            GameObject panelRoot = rig.AddChild("Panel");
            panelRoot.SetActive(false);
            rig.FullMap.SetManager(manager);
            rig.FullMap.PreviewPanelSlots.SetPanelRoot(panelRoot);
            rig.Rebind();

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
            GameObject panelRoot = rig.AddChild("Panel");
            GameObject confirmObject = CreateChild(panelRoot, "Confirm");
            Button confirmButton = confirmObject.AddComponent<Button>();

            rig.FullMap.SetManager(manager);
            rig.FullMap.PreviewPanelSlots.SetPanelRoot(panelRoot);
            rig.FullMap.PreviewPanelSlots.SetConfirmButton(confirmButton);
            rig.Rebind();

            manager.PreviewDestination(new Vector3(60f, 0f, 0f));
            yield return null;

            confirmButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator StopButton_VisibleOnlyWithActiveRoute()
        {
            Button stopButton = rig.AddButton("Stop");
            GameObject stopObject = stopButton.gameObject;

            rig.FullMap.SetManager(manager);
            rig.FullMap.Buttons.SetStopButton(stopButton);
            rig.Rebind();
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
            Button centerButton = rig.AddButton("Center");
            GameObject centerObject = centerButton.gameObject;

            rig.FullMap.SetManager(manager);
            rig.FullMap.Buttons.SetCenterButton(centerButton);
            rig.Rebind();
            yield return null;

            Assert.IsFalse(centerObject.activeSelf);

            interactive.Pan(new Vector2(20f, 0f));
            yield return null;

            Assert.IsTrue(centerObject.activeSelf);

            interactive.CenterOnCar();
            yield return null;

            Assert.IsFalse(centerObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_TapOnEmptyMap_NothingHappens()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            manager.PreviewReady += HandlePreviewReady;
            manager.PreviewFailed += HandlePreviewFailed;

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsFalse(previewReadyRaised);
            Assert.IsFalse(previewFailedRaised);
            Assert.IsFalse(manager.HasPreview);
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_TapNearDestinationMarker_PreviewsMarker()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            manager.PreviewReady += HandlePreviewReady;

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreSame(marker, reportedMarker);
            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_TapNearNonDestinationMarker_NothingHappens()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.None);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_TapOnEmptyMap_KeepsExistingPreview()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(manager.HasPreview);
            Vector3 previewPosition = manager.Markers.GetEntry(manager.PreviewMarkerIndex).TruePosition;

            interactive.TapAt(ComputeViewportPoint(new Vector3(100f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(manager.HasPreview);
            Vector3 newPreviewPosition = manager.Markers.GetEntry(manager.PreviewMarkerIndex).TruePosition;
            Assert.Less(Vector3.Distance(previewPosition, newPreviewPosition), 0.01f);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_ConfirmStepOff_TapOnEmptyMap_NoNavigation()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            rig.FullMap.InteractionSettings.SetConfirmStep(false);
            manager.NavigationStarted += HandleNavigationStarted;

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsFalse(navigationStartedRaised);
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_ConfirmAtCrosshair_NoMarker_NothingHappens()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            interactive.SetCrosshairMode(true);

            interactive.ConfirmAtCrosshair();
            yield return null;

            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator MapAndMarkers_TapOnEmptyMap_StillPreviews()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MapAndMarkers);

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator TapSelectMarker_SelectsWithoutPreview()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreSame(marker, manager.SelectedMarker);
            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator TapDestinationMarker_SelectsThenPreviews()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("Selected:" + marker.name, events[0]);
            Assert.AreEqual("PreviewReady", events[1]);
            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator TapOtherMarker_DeselectsThenSelects()
        {
            MapMarker markerA = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            markerA.name = "A";
            MapMarker markerB = CreateMarker(new Vector3(100f, 0f, 0f), MarkerTapAction.Select);
            markerB.name = "B";
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;
            interactive.TapAt(ComputeViewportPoint(new Vector3(100f, 0f, 0f)));
            yield return null;

            Assert.AreEqual(3, events.Count);
            Assert.AreEqual("Selected:A", events[0]);
            Assert.AreEqual("Deselected:A", events[1]);
            Assert.AreEqual("Selected:B", events[2]);
            Assert.AreSame(markerB, manager.SelectedMarker);
        }

        [UnityTest]
        public IEnumerator TapSameMarkerTwice_OneSelectedEvent()
        {
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;
            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            Assert.AreEqual(1, events.Count);
        }

        [UnityTest]
        public IEnumerator TapEmptyMap_ClearsSelectionAndPreviews()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MapAndMarkers);
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;
            Assert.IsNotNull(manager.SelectedMarker);

            interactive.TapAt(ComputeViewportPoint(new Vector3(100f, 0f, 0f)));
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator MarkersOnly_TapEmptyMap_ClearsSelectionKeepsPreview()
        {
            rig.FullMap.InteractionSettings.SetTapTarget(FullMapTapTarget.MarkersOnly);
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;
            Assert.IsNotNull(manager.SelectedMarker);
            Assert.IsTrue(manager.HasPreview);

            interactive.TapAt(ComputeViewportPoint(new Vector3(100f, 0f, 0f)));
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
            Assert.IsTrue(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator DisableSelectedMarker_Deselects()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;

            marker.gameObject.SetActive(false);
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
            Assert.AreEqual("Deselected:" + marker.name, events[events.Count - 1]);
        }

        [UnityTest]
        public IEnumerator CloseFullMap_ClearsSelection()
        {
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 0f)));
            yield return null;
            Assert.IsNotNull(manager.SelectedMarker);

            rig.Root.SetActive(false);
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
        }

        [UnityTest]
        public IEnumerator ClearSelection_NothingSelected_NoEvent()
        {
            SubscribeSelectionEvents();

            manager.ClearSelection();
            yield return null;

            Assert.AreEqual(0, events.Count);
        }

        [UnityTest]
        public IEnumerator ClearSelectionInsideHandler_IsQueued()
        {
            CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();
            manager.PreviewReady += HandlePreviewReadyClearSelection;

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreEqual(3, events.Count);
            Assert.IsTrue(events[0].StartsWith("Selected:"));
            Assert.AreEqual("PreviewReady", events[1]);
            Assert.IsTrue(events[2].StartsWith("Deselected:"));
            Assert.IsNull(manager.SelectedMarker);
        }

        [UnityTest]
        public IEnumerator DestinationMarker_Cancel_Deselects()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;
            manager.CancelPreview();
            yield return null;

            Assert.AreEqual(4, events.Count);
            Assert.AreEqual("Selected:" + marker.name, events[0]);
            Assert.AreEqual("PreviewReady", events[1]);
            Assert.AreEqual("PreviewCanceled", events[2]);
            Assert.AreEqual("Deselected:" + marker.name, events[3]);
            Assert.IsNull(manager.SelectedMarker);
        }

        [UnityTest]
        public IEnumerator DestinationMarker_Confirm_Deselects()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;
            manager.ConfirmPreview();
            yield return null;

            Assert.AreEqual(4, events.Count);
            Assert.AreEqual("Selected:" + marker.name, events[0]);
            Assert.AreEqual("PreviewReady", events[1]);
            Assert.AreEqual("NavigationStarted", events[2]);
            Assert.AreEqual("Deselected:" + marker.name, events[3]);
            Assert.IsNull(manager.SelectedMarker);
            Assert.IsTrue(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator DestinationMarker_ConfirmStepOff_SelectStartDeselect()
        {
            rig.FullMap.InteractionSettings.SetConfirmStep(false);
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;

            Assert.AreEqual(3, events.Count);
            Assert.AreEqual("Selected:" + marker.name, events[0]);
            Assert.AreEqual("NavigationStarted", events[1]);
            Assert.AreEqual("Deselected:" + marker.name, events[2]);
            Assert.IsNull(manager.SelectedMarker);
            Assert.IsTrue(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator SelectMarker_CodePreview_CancelKeepsSelection()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), MarkerTapAction.Select);
            yield return WaitFrames(3);

            interactive.TapAt(ComputeViewportPoint(new Vector3(62f, 0f, 0f)));
            yield return null;
            Assert.AreSame(marker, manager.SelectedMarker);

            manager.PreviewDestination(new Vector3(100f, 0f, 0f));
            yield return null;
            Assert.IsTrue(manager.HasPreview);
            Assert.AreSame(marker, manager.SelectedMarker);

            manager.CancelPreview();
            yield return null;

            Assert.IsFalse(manager.HasPreview);
            Assert.AreSame(marker, manager.SelectedMarker);
        }

        [UnityTest]
        public IEnumerator DestinationMarker_PreviewFails_KeepsSelection()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 80f), MarkerTapAction.Destination);
            yield return WaitFrames(3);
            SubscribeSelectionEvents();

            interactive.TapAt(ComputeViewportPoint(new Vector3(60f, 0f, 80f)));
            yield return null;

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("Selected:" + marker.name, events[0]);
            Assert.AreEqual("PreviewFailed", events[1]);
            Assert.IsFalse(manager.HasPreview);
            Assert.AreSame(marker, manager.SelectedMarker);
        }

        private void SubscribeSelectionEvents()
        {
            manager.MarkerSelected += HandleMarkerSelected;
            manager.MarkerDeselected += HandleMarkerDeselected;
            manager.PreviewReady += HandlePreviewReadyLogged;
            manager.PreviewCanceled += HandlePreviewCanceledLogged;
            manager.PreviewFailed += HandlePreviewFailedLogged;
            manager.NavigationStarted += HandleNavigationStartedLogged;
        }

        private void HandlePreviewCanceledLogged()
        {
            events.Add("PreviewCanceled");
        }

        private void HandlePreviewFailedLogged(FailureReason reason)
        {
            events.Add("PreviewFailed");
        }

        private void HandleNavigationStartedLogged(Route route)
        {
            events.Add("NavigationStarted");
        }

        private void HandleMarkerSelected(MapMarker marker)
        {
            events.Add("Selected:" + marker.name);
        }

        private void HandleMarkerDeselected(MapMarker marker)
        {
            events.Add("Deselected:" + marker.name);
        }

        private void HandlePreviewReadyLogged(Route route, MapMarker marker)
        {
            events.Add("PreviewReady");
        }

        private void HandlePreviewReadyClearSelection(Route route, MapMarker marker)
        {
            manager.ClearSelection();
        }

        private void HandlePreviewReady(Route route, MapMarker marker)
        {
            previewReadyRaised = true;
            reportedMarker = marker;
        }

        private void HandlePreviewFailed(FailureReason reason)
        {
            previewFailedRaised = true;
        }

        private void HandleNavigationStarted(Route route)
        {
            navigationStartedRaised = true;
        }

        private Vector2 ComputeViewportPoint(Vector3 truePosition)
        {
            Vector2 mapPoint = mapData.CreateFrame().TrueToMap(truePosition);
            Vector2 delta = mapPoint - view.CenterMap;
            return delta * view.CanvasUnitsPerMeter;
        }

        private MapMarker CreateMarker(Vector3 position, MarkerTapAction tapAction)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetTapAction(tapAction);
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

        private IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
