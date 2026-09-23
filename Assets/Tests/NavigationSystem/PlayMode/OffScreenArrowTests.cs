using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class OffScreenArrowTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject markerTemplate;
        private GameObject arrowTemplate;
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

            arrowTemplate = new GameObject("ArrowTemplate", typeof(RectTransform));
            arrowTemplate.AddComponent<FakeTextTarget>();

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            car = CreateCar("TestCar", new Vector3(20f, 0f, 0f));

            CreateMap();
            CreateManager();

            view = viewObject.AddComponent<MapView>();
            view.SetArrowPrefab(arrowTemplate);
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
            Object.DestroyImmediate(arrowTemplate);

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
        public IEnumerator DestinationOutOfView_ArrowShown()
        {
            yield return WaitFrames(2);

            view.SetCenter(new Vector2(70f, 100f));
            view.SetZoomMeters(100f, 999999f);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitFrames(3);

            int index = manager.DestinationMarkerIndex;
            Assert.GreaterOrEqual(index, 0);
            Assert.IsNotNull(view.MarkerLayer.GetActiveArrow(index));
            Assert.IsNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator DestinationInView_NoArrow()
        {
            yield return WaitFrames(2);

            view.SetCenter(new Vector2(200f, 100f));
            view.SetZoomMeters(999999f, 999999f);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitFrames(3);

            int index = manager.DestinationMarkerIndex;
            Assert.GreaterOrEqual(index, 0);
            Assert.IsNull(view.MarkerLayer.GetActiveArrow(index));
            Assert.IsNotNull(view.MarkerLayer.GetActiveInstance(index));
        }

        [UnityTest]
        public IEnumerator ShowArrowsOff_NoArrow()
        {
            yield return WaitFrames(2);

            view.SetShowOffScreenArrows(false);
            view.SetCenter(new Vector2(70f, 100f));
            view.SetZoomMeters(100f, 999999f);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitFrames(3);

            int index = manager.DestinationMarkerIndex;
            Assert.GreaterOrEqual(index, 0);
            Assert.IsNull(view.MarkerLayer.GetActiveArrow(index));
        }

        [UnityTest]
        public IEnumerator DistanceLabel_UpdatesOnlyWhenValueChanges()
        {
            yield return WaitFrames(2);

            view.SetCenter(new Vector2(70f, 100f));
            view.SetZoomMeters(100f, 999999f);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitFrames(3);

            int index = manager.DestinationMarkerIndex;
            GameObject arrow = view.MarkerLayer.GetActiveArrow(index);
            Assert.IsNotNull(arrow);

            FakeTextTarget textTarget = arrow.GetComponent<FakeTextTarget>();
            int callCountAfterShow = textTarget.SetTextCallCount;
            Assert.Greater(callCountAfterShow, 0);

            car.position = new Vector3(24f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.AreEqual(callCountAfterShow, textTarget.SetTextCallCount);

            car.position = new Vector3(50f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.Greater(textTarget.SetTextCallCount, callCountAfterShow);
        }

        [UnityTest]
        public IEnumerator ChildLabel_MovedToLabelLayer_Upright()
        {
            GameObject labelledArrow = new GameObject("LabelledArrowTemplate", typeof(RectTransform));
            GameObject label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(labelledArrow.transform, false);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -20f);
            label.AddComponent<FakeTextTarget>();
            createdObjects.Add(labelledArrow);
            view.SetArrowPrefab(labelledArrow);
            yield return WaitFrames(2);

            view.SetCenter(new Vector2(70f, 100f));
            view.SetZoomMeters(100f, 999999f);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            yield return WaitFrames(3);

            GameObject arrow = view.MarkerLayer.GetActiveArrow(manager.DestinationMarkerIndex);
            Assert.IsNotNull(arrow);
            Assert.AreEqual(0, arrow.GetComponentsInChildren<FakeTextTarget>(true).Length);

            Transform labelLayer = view.MarkerLayer.transform.Find("OffScreenArrowLabels");
            Assert.IsNotNull(labelLayer);
            Assert.Greater(labelLayer.GetSiblingIndex(), arrow.transform.parent.GetSiblingIndex());

            FakeTextTarget labelInstance = labelLayer.GetComponentInChildren<FakeTextTarget>(true);
            Assert.IsNotNull(labelInstance);
            Assert.IsTrue(labelInstance.gameObject.activeSelf);
            Assert.Greater(labelInstance.SetTextCallCount, 0);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, labelInstance.transform.localRotation), 0.01f);

            Vector2 arrowPosition = ((RectTransform)arrow.transform).anchoredPosition;
            Vector2 labelPosition = ((RectTransform)labelInstance.transform).anchoredPosition;
            Assert.AreEqual(20f, Vector2.Distance(arrowPosition, labelPosition), 0.01f);
        }

        private Transform CreateCar(string name, Vector3 position)
        {
            GameObject carObject = new GameObject(name);
            carObject.transform.position = position;
            carObject.transform.rotation = Quaternion.LookRotation(Vector3.right);
            createdObjects.Add(carObject);
            return carObject.transform;
        }

        private void CreateManager()
        {
            GameObject managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);
            manager.SetPlayerMarkerPrefab(markerTemplate);
            manager.SetDestinationMarkerPrefab(markerTemplate);
        }

        private void CreateMap()
        {
            Vector3 center = new Vector3(150f, 0f, 0f);

            MapData data = ScriptableObject.CreateInstance<MapData>();
            data.SetRectangleCenter(center);
            data.SetRectangleSize(new Vector2(400f, 200f));
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

        private class FakeTextTarget : NavigationTextTarget
        {
            public int SetTextCallCount { get; private set; }

            public override void SetText(StringBuilder text)
            {
                SetTextCallCount++;
            }
        }
    }
}
