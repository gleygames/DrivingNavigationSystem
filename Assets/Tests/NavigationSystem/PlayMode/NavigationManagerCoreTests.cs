using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationManagerCoreTests
    {
        private const float Tolerance = 0.01f;

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
        public IEnumerator SingleMap_ActivatedAutomatically()
        {
            CreateManager(false);
            NavigationMap map = CreateMap("Map");

            yield return WaitFrames(2);

            Assert.IsTrue(manager.IsInitialized);
            Assert.AreSame(map, manager.ActiveMap);
        }

        [UnityTest]
        public IEnumerator TwoMaps_NoneChosen_LogsWarning_NoActiveMap()
        {
            CreateMap("MapA");
            CreateMap("MapB");
            LogAssert.Expect(LogType.Warning, new Regex("several maps loaded"));
            CreateManager(false);

            yield return WaitFrames(2);

            Assert.IsTrue(manager.IsInitialized);
            Assert.IsNull(manager.ActiveMap);
        }

        [UnityTest]
        public IEnumerator ExplicitMap_WinsOverAutomatic()
        {
            CreateMap("MapA");
            NavigationMap second = CreateMap("MapB");
            CreateManager(false);
            manager.SetExplicitMap(second);

            yield return WaitFrames(2);

            Assert.AreSame(second, manager.ActiveMap);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MapCreatedBeforeManager_RegisteredOnInitialize()
        {
            NavigationMap map = CreateMap("Map");
            CreateManager(false);

            yield return WaitFrames(2);

            Assert.AreSame(map, manager.ActiveMap);
        }

        [UnityTest]
        public IEnumerator ActiveMapDisabled_OtherSingleMapActivated()
        {
            CreateManager(false);
            NavigationMap first = CreateMap("MapA");
            yield return WaitFrames(2);
            Assert.AreSame(first, manager.ActiveMap);

            NavigationMap second = CreateMap("MapB");
            yield return null;
            Assert.AreSame(first, manager.ActiveMap);

            first.gameObject.SetActive(false);
            yield return null;

            Assert.AreSame(second, manager.ActiveMap);
        }

        [UnityTest]
        public IEnumerator StartManually_NotInitializedUntilInitializeCalled()
        {
            CreateManager(true);
            NavigationMap map = CreateMap("Map");

            yield return WaitFrames(3);

            Assert.IsFalse(manager.IsInitialized);
            Assert.IsNull(manager.ActiveMap);

            manager.Initialize();

            Assert.IsTrue(manager.IsInitialized);
            Assert.AreSame(map, manager.ActiveMap);
        }

        [UnityTest]
        public IEnumerator CarMovesAlongRoad_CurrentRoadIdMatches()
        {
            CreateManager(false);
            CreateMap("Map");
            yield return WaitFrames(2);

            Assert.AreEqual(1, manager.CurrentRoadId);

            for (int i = 0; i < 24; i++)
            {
                car.position += new Vector3(5f, 0f, 0f);
                yield return null;
            }

            Assert.AreEqual(2, manager.CurrentRoadId);
            Assert.IsFalse(manager.IsOffRoad);
        }

        [UnityTest]
        public IEnumerator CarMovesAway_IsOffRoadTrue()
        {
            car.position = new Vector3(50f, 0f, 0f);
            CreateManager(false);
            CreateMap("Map");
            yield return WaitFrames(2);

            Assert.IsFalse(manager.IsOffRoad);

            for (int i = 0; i < 12; i++)
            {
                car.position += new Vector3(0f, 0f, 5f);
                yield return null;
            }

            Assert.IsTrue(manager.IsOffRoad);
            Assert.AreEqual(-1, manager.CurrentRoadId);
        }

        [UnityTest]
        public IEnumerator YawOffset90_NoseHeadingRotated()
        {
            car.rotation = Quaternion.identity;
            CreateManager(false);
            manager.SetCarReference(car, 90f);
            CreateMap("Map");

            yield return WaitFrames(2);

            Vector3 nose = manager.NoseHeading;
            Assert.AreEqual(1f, nose.x, Tolerance);
            Assert.AreEqual(0f, nose.y, Tolerance);
            Assert.AreEqual(0f, nose.z, Tolerance);
        }

        [UnityTest]
        public IEnumerator RectangleShift_MapObjectMoved_TruePositionUnchanged()
        {
            CreateManager(false);
            NavigationMap map = CreateMap("Map");
            yield return WaitFrames(2);

            for (int i = 0; i < 5; i++)
            {
                car.position += new Vector3(2f, 0f, 0f);
                yield return null;
            }

            float trueXBefore = manager.CarTruePosition.x;
            Vector3 shift = new Vector3(1000f, 0f, 0f);
            car.position += shift + new Vector3(2f, 0f, 0f);
            map.transform.position += shift;
            yield return null;

            Assert.AreEqual(trueXBefore + 2f, manager.CarTruePosition.x, Tolerance);
            Assert.AreEqual(0f, manager.CarTruePosition.z, Tolerance);
            Assert.IsFalse(manager.CarTeleported);
            Assert.IsFalse(manager.IsOffRoad);
            Assert.AreEqual(1, manager.CurrentRoadId);
        }

        [UnityTest]
        public IEnumerator ManualShift_OnOriginShifted_Accumulates()
        {
            CreateManager(false);
            manager.SetShiftSource(ShiftSource.Manual);
            NavigationMap map = CreateMap("Map");
            yield return WaitFrames(2);

            Vector3 firstDelta = new Vector3(100f, 0f, 0f);
            Vector3 secondDelta = new Vector3(50f, 0f, 10f);
            manager.OnOriginShifted(firstDelta);
            manager.OnOriginShifted(secondDelta);

            Vector3 shift = manager.Converter.Shift;
            Assert.AreEqual(150f, shift.x, Tolerance);
            Assert.AreEqual(0f, shift.y, Tolerance);
            Assert.AreEqual(10f, shift.z, Tolerance);

            car.position += firstDelta + secondDelta;
            map.transform.position += firstDelta + secondDelta;
            yield return null;

            Assert.AreEqual(20f, manager.CarTruePosition.x, Tolerance);
            Assert.AreEqual(0f, manager.CarTruePosition.z, Tolerance);
            Assert.IsFalse(manager.CarTeleported);
        }

        [UnityTest]
        public IEnumerator TimeScaleZero_NoLogicUpdate()
        {
            CreateManager(false);
            CreateMap("Map");
            yield return WaitFrames(2);

            Time.timeScale = 0f;
            yield return null;

            Assert.AreEqual(0f, Time.deltaTime);
            float trueXBefore = manager.CarTruePosition.x;
            car.position += new Vector3(5f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.AreEqual(trueXBefore, manager.CarTruePosition.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator RectangleRotatedAtRuntime_LogsWarningOnce()
        {
            CreateManager(false);
            NavigationMap map = CreateMap("Map");
            yield return WaitFrames(2);

            LogAssert.Expect(LogType.Warning, new Regex("rotated at runtime"));
            map.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            yield return WaitFrames(3);

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Speed_MatchesCarMovement()
        {
            CreateManager(false);
            CreateMap("Map");
            yield return WaitFrames(2);

            for (int i = 0; i < 3; i++)
            {
                car.position += new Vector3(0.5f, 0f, 0f);
                yield return null;
            }

            float deltaTime = Time.deltaTime;
            car.position += new Vector3(0.5f, 0f, 0f);
            yield return null;

            float expected = 0.5f / deltaTime;
            Assert.AreEqual(expected, manager.Speed, expected * 0.01f);
        }

        [UnityTest]
        public IEnumerator CarOutsideRectangle_IsOutsideMapTrue()
        {
            CreateManager(false);
            CreateMap("Map");
            yield return WaitFrames(2);

            Assert.IsFalse(manager.IsOutsideMap);

            car.position = new Vector3(400f, 0f, 0f);
            yield return null;

            Assert.IsTrue(manager.IsOutsideMap);
        }

        private void CreateManager(bool startManually)
        {
            managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);
            manager.SetStartManually(startManually);
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
