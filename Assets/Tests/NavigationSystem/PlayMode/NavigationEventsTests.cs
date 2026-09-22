using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationEventsTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private NavigationEvents events;
        private Transform car;
        private int arrivedCount;
        private int routeFailedCount;
        private FailureReason lastRouteFailure;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            arrivedCount = 0;
            routeFailedCount = 0;
            lastRouteFailure = FailureReason.None;

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            GameObject carObject = new GameObject("TestCar");
            car = carObject.transform;
            car.position = new Vector3(20f, 0f, 0f);
            car.rotation = Quaternion.LookRotation(Vector3.right);
            createdObjects.Add(carObject);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

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

            if (network.Settings != null)
            {
                Object.DestroyImmediate(network.Settings);
            }
            Object.DestroyImmediate(network);
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator Arrived_ForwardedToUnityEvent()
        {
            CreateMap("Map");
            CreateManagerWithEvents();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));

            for (int i = 0; i < 60; i++)
            {
                if (arrivedCount > 0)
                {
                    break;
                }
                car.position += new Vector3(5f, 0f, 0f);
                yield return null;
            }

            Assert.AreEqual(1, arrivedCount);
        }

        [UnityTest]
        public IEnumerator RouteFailed_ForwardsReason()
        {
            CreateMap("Map");
            CreateManagerWithEvents();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(150f, 0f, 90f));
            yield return null;

            Assert.AreEqual(1, routeFailedCount);
            Assert.AreEqual(FailureReason.NoRoadNearDestination, lastRouteFailure);
        }

        [UnityTest]
        public IEnumerator Disabled_NoLongerForwards()
        {
            CreateMap("Map");
            CreateManagerWithEvents();
            yield return WaitFrames(2);

            events.enabled = false;
            yield return null;

            manager.StartNavigation(new Vector3(280f, 0f, 0f));

            for (int i = 0; i < 60; i++)
            {
                car.position += new Vector3(5f, 0f, 0f);
                yield return null;
            }

            Assert.AreEqual(0, arrivedCount);
        }

        private void CreateManagerWithEvents()
        {
            GameObject managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);

            events = managerObject.AddComponent<NavigationEvents>();
            events.Arrived.AddListener(OnArrived);
            events.RouteFailed.AddListener(OnRouteFailed);
        }

        private void OnArrived()
        {
            arrivedCount++;
        }

        private void OnRouteFailed(FailureReason reason)
        {
            routeFailedCount++;
            lastRouteFailure = reason;
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

        private IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
