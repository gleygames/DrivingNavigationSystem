using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapMarkerTests
    {
        private const int FullMapBit = 1 << 1;

        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private Transform car;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            car = CreateCar("TestCar", new Vector3(20f, 0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
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

            DestroyNetwork();
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator MapMarker_EnableDisable_RegistersAndUnregisters()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            MapMarker marker = CreateMarker(new Vector3(100f, 0f, 0f));

            Assert.IsTrue(HasMarker(marker));

            marker.enabled = false;
            Assert.IsFalse(HasMarker(marker));

            marker.enabled = true;
            Assert.IsTrue(HasMarker(marker));
        }

        [UnityTest]
        public IEnumerator MarkerCreatedBeforeManager_AddedOnInitialize()
        {
            CreateMap("Map");

            MapMarker marker = CreateMarker(new Vector3(100f, 0f, 0f));

            CreateManager();
            yield return WaitFrames(2);

            Assert.IsTrue(HasMarker(marker));
        }

        [UnityTest]
        public IEnumerator NavigationStarted_DestinationPointAdded()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            Assert.AreEqual(-1, manager.DestinationMarkerIndex);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));

            Assert.GreaterOrEqual(manager.DestinationMarkerIndex, 0);
            MarkerEntry entry = manager.Markers.GetEntry(manager.DestinationMarkerIndex);
            Assert.IsTrue(entry.Alive);
            Assert.IsTrue(entry.ShowArrow);
            Assert.AreEqual(280f, entry.TruePosition.x, 1f);
        }

        [UnityTest]
        public IEnumerator Arrived_DestinationRemoved()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(60f, 0f, 0f));
            Assert.GreaterOrEqual(manager.DestinationMarkerIndex, 0);

            yield return DriveCar(new Vector3(5f, 0f, 0f), 8);

            Assert.AreEqual(-1, manager.DestinationMarkerIndex);
        }

        [UnityTest]
        public IEnumerator PreviewPin_OnlyFullMapChannel()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));

            Assert.GreaterOrEqual(manager.PreviewMarkerIndex, 0);
            MarkerEntry entry = manager.Markers.GetEntry(manager.PreviewMarkerIndex);
            Assert.AreEqual(FullMapBit, entry.ChannelMask);
        }

        [UnityTest]
        public IEnumerator SetCar_PlayerMarkerRetargeted()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            int playerIndex = manager.Markers.PlayerIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            Transform newCar = CreateCar("NewCar", new Vector3(120f, 0f, 0f));
            manager.SetCar(newCar);
            yield return null;

            MarkerEntry entry = manager.Markers.GetEntry(playerIndex);
            Assert.AreEqual(120f, entry.TruePosition.x, 1f);
        }

        [UnityTest]
        public IEnumerator PlayerHeading_UsesNoseWithYawOffset()
        {
            CreateMap("Map");
            CreateManager(90f);
            yield return WaitFrames(2);

            int playerIndex = manager.Markers.PlayerIndex;
            MarkerEntry entry = manager.Markers.GetEntry(playerIndex);

            Vector3 expectedHeading = (car.rotation * Quaternion.Euler(0f, 90f, 0f) * Vector3.forward).normalized;
            Assert.AreEqual(expectedHeading.x, entry.TrueHeading.x, 0.01f);
            Assert.AreEqual(expectedHeading.z, entry.TrueHeading.z, 0.01f);
            Assert.IsFalse(Mathf.Approximately(car.forward.x, entry.TrueHeading.x));
        }

        private bool HasMarker(MapMarker marker)
        {
            for (int i = 0; i < manager.Markers.EntryCount; i++)
            {
                MarkerEntry entry = manager.Markers.GetEntry(i);
                if (entry.Marker == marker && entry.Alive)
                {
                    return true;
                }
            }
            return false;
        }

        private MapMarker CreateMarker(Vector3 position)
        {
            GameObject markerObject = new GameObject("Marker");
            markerObject.transform.position = position;
            MapMarker marker = markerObject.AddComponent<MapMarker>();
            createdObjects.Add(markerObject);
            return marker;
        }

        private void CreateManager()
        {
            CreateManager(0f);
        }

        private void CreateManager(float yawOffset)
        {
            GameObject managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, yawOffset);
        }

        private Transform CreateCar(string name, Vector3 position)
        {
            GameObject carObject = new GameObject(name);
            carObject.transform.position = position;
            carObject.transform.rotation = Quaternion.LookRotation(Vector3.right);
            createdObjects.Add(carObject);
            return carObject.transform;
        }

        private NavigationMap CreateMap(string name)
        {
            Vector3 center = new Vector3(150f, 0f, 0f);

            MapData data = ScriptableObject.CreateInstance<MapData>();
            data.SetRectangleCenter(center);
            data.SetRectangleSize(new Vector2(400f, 200f));
            data.SetRectangleRotationY(0f);
            data.SetEditTimeWorldPosition(center);
            data.SetRoadNetwork(network);
            mapAssets.Add(data);

            GameObject mapObject = new GameObject(name);
            mapObject.SetActive(false);
            mapObject.transform.position = center;
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            map.SetMapData(data);
            createdObjects.Add(mapObject);
            mapObject.SetActive(true);

            return map;
        }

        private void DestroyNetwork()
        {
            if (network == null)
            {
                return;
            }
            if (network.Settings != null)
            {
                Object.DestroyImmediate(network.Settings);
            }
            Object.DestroyImmediate(network);
            network = null;
        }

        private IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private IEnumerator DriveCar(Vector3 step, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                car.position += step;
                yield return null;
            }
        }
    }
}
