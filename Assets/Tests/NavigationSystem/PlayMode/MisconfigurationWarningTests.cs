using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MisconfigurationWarningTests
    {
        private readonly List<GameObject> maps = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();

        private NavigationSettings settings;
        private RoadNetworkData network;
        private GameObject managerObject;
        private GameObject carObject;
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

            carObject = new GameObject("TestCar");
            car = carObject.transform;
            car.position = new Vector3(20f, 0f, 0f);
            car.rotation = Quaternion.LookRotation(Vector3.right);

            managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);
            manager.SetStartManually(false);
            CreateMap("Map");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            if (managerObject != null)
            {
                Object.DestroyImmediate(managerObject);
            }
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    Object.DestroyImmediate(maps[i]);
                }
            }
            maps.Clear();
            Object.DestroyImmediate(carObject);

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
        public IEnumerator CarShiftedButMapNot_LogsWarningOnce()
        {
            yield return WaitFrames(2);
            Assert.AreNotEqual(-1, manager.CurrentRoadId);

            LogAssert.Expect(LogType.Warning, new Regex("jumped far away from all roads"));
            car.position += new Vector3(5000f, 0f, 0f);
            yield return null;

            car.position += new Vector3(5000f, 0f, 0f);
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NormalTeleportToRoad_NoWarning()
        {
            yield return WaitFrames(2);
            Assert.AreNotEqual(-1, manager.CurrentRoadId);

            car.position = new Vector3(250f, 0f, 0f);
            yield return null;

            Assert.IsTrue(manager.CarTeleported);
            Assert.IsFalse(manager.IsOffRoad);
            LogAssert.NoUnexpectedReceived();
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
            maps.Add(mapObject);
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
