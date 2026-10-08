using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class InfoPanelTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        private GameObject canvasObject;
        private FullMapTestRig rig;
        private GameObject managerObject;
        private GameObject carObject;
        private GameObject mapObject;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private MapData mapData;
        private Texture2D mapTexture;
        private GameObject panelRoot;
        private Text titleText;
        private Button closeButton;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
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
            rig.FullMap.SetManager(manager);

            panelRoot = rig.AddChild("InfoPanel");
            panelRoot.SetActive(false);
            GameObject titleObject = CreateChild(panelRoot, "Title");
            titleText = titleObject.AddComponent<Text>();
            GameObject closeObject = CreateChild(panelRoot, "Close");
            closeObject.AddComponent<Image>();
            closeButton = closeObject.AddComponent<Button>();

            rig.Activate();

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
        public IEnumerator SelectNamedMarker_PanelShowsName()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");

            manager.SelectMarker(marker);
            yield return null;

            Assert.IsTrue(panelRoot.activeSelf);
            Assert.AreEqual("Acme", titleText.text);
        }

        [UnityTest]
        public IEnumerator SelectUnnamedMarker_PanelHidden()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "");

            manager.SelectMarker(marker);
            yield return null;

            Assert.IsFalse(panelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator Deselect_PanelHidden()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");
            manager.SelectMarker(marker);
            yield return null;
            Assert.IsTrue(panelRoot.activeSelf);

            manager.ClearSelection();
            yield return null;

            Assert.IsFalse(panelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator CloseButton_ClearsSelection()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");
            manager.SelectMarker(marker);
            yield return null;

            closeButton.onClick.Invoke();
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
            Assert.IsFalse(panelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator RenameSelected_TitleUpdates()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");
            manager.SelectMarker(marker);
            yield return null;

            marker.DisplayName = "Globex";
            yield return null;

            Assert.IsTrue(panelRoot.activeSelf);
            Assert.AreEqual("Globex", titleText.text);
        }

        [UnityTest]
        public IEnumerator RenameOther_TitleUnchanged()
        {
            AssignSlots();
            MapMarker selected = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");
            MapMarker other = CreateMarker(new Vector3(100f, 0f, 0f), "Globex");
            manager.SelectMarker(selected);
            yield return null;

            other.DisplayName = "Initech";
            yield return null;

            Assert.AreEqual("Acme", titleText.text);
        }

        [UnityTest]
        public IEnumerator ReopenFullMap_PanelHidden()
        {
            AssignSlots();
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");
            manager.SelectMarker(marker);
            yield return null;
            Assert.IsTrue(panelRoot.activeSelf);

            rig.Root.SetActive(false);
            yield return null;
            rig.Root.SetActive(true);
            yield return null;

            Assert.IsNull(manager.SelectedMarker);
            Assert.IsFalse(panelRoot.activeSelf);
        }

        [UnityTest]
        public IEnumerator EmptySlots_NoErrors()
        {
            MapMarker marker = CreateMarker(new Vector3(60f, 0f, 0f), "Acme");

            manager.SelectMarker(marker);
            yield return null;
            marker.DisplayName = "Globex";
            yield return null;
            manager.ClearSelection();
            yield return null;

            LogAssert.NoUnexpectedReceived();
            Assert.IsNull(manager.SelectedMarker);
        }

        private void AssignSlots()
        {
            rig.FullMap.InfoPanelSlots.SetPanelRoot(panelRoot);
            rig.FullMap.InfoPanelSlots.SetTitleText(titleText);
            rig.FullMap.InfoPanelSlots.SetCloseButton(closeButton);
            rig.Rebind();
        }

        private MapMarker CreateMarker(Vector3 position, string displayName)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.SetActive(false);
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            createdObjects.Add(markerObject);
            markerObject.SetActive(true);
            marker.DisplayName = displayName;
            return marker;
        }

        private GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }
    }
}
