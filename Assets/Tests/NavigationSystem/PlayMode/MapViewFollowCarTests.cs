using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewFollowCarTests
    {
        private const float Tolerance = 0.01f;

        private GameObject canvasObject;
        private GameObject viewObject;
        private GameObject managerObject;
        private GameObject carObject;
        private GameObject mapObject;
        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private MapView view;
        private MapViewFollowCar followCar;
        private MapData mapData;
        private Texture2D mapTexture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            viewObject = new GameObject("Minimap", typeof(RectTransform));
            viewObject.transform.SetParent(canvasObject.transform, false);
            RectTransform viewRect = viewObject.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.5f, 0.5f);
            viewRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.sizeDelta = new Vector2(200f, 200f);

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
            mapData.SetRectangleSize(new Vector2(400f, 200f));
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
            followCar = viewObject.AddComponent<MapViewFollowCar>();
            followCar.SetRotationSmoothing(0f);
            followCar.SetTurnSmoothing(0f);
            followCar.SetZoomSmoothing(0f);
            followCar.SetSpeedZoom(false);
            followCar.SetFixedZoomMeters(100f);
            followCar.SetRound(true);

            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
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
        public IEnumerator HeadingUp_Reversing_RotationUnchanged()
        {
            followCar.SetRotationMode(MinimapRotationMode.HeadingUp);

            for (int i = 0; i < 5; i++)
            {
                carObject.transform.position += new Vector3(2f, 0f, 0f);
                yield return null;
            }

            AssertNosePointsUp();
            Vector2 carLocal = view.MapToViewport(manager.CarMapPosition);
            Assert.AreEqual(0f, carLocal.x, 0.5f);
            Assert.AreEqual(-40f, carLocal.y, 0.5f);

            for (int i = 0; i < 5; i++)
            {
                carObject.transform.position -= new Vector3(2f, 0f, 0f);
                yield return null;
            }

            Assert.Less(manager.MovementHeading.x, 0f);
            AssertNosePointsUp();
        }

        [UnityTest]
        public IEnumerator HeadingUp_WeavingOnRoad_RotationFollowsRoad()
        {
            followCar.SetRotationMode(MinimapRotationMode.HeadingUp);

            for (int i = 0; i < 10; i++)
            {
                float yaw = 80f;
                if (i % 2 == 1)
                {
                    yaw = 100f;
                }
                carObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                carObject.transform.position += new Vector3(2f, 0f, 0f);
                yield return null;

                if (i >= 2)
                {
                    Assert.IsTrue(manager.HasRoadHeading);
                    Assert.AreEqual(90f, view.RotationDegrees, Tolerance);
                }
            }
        }

        [UnityTest]
        public IEnumerator HeadingUp_OffRoad_FollowsNose()
        {
            followCar.SetRotationMode(MinimapRotationMode.HeadingUp);
            carObject.transform.position = new Vector3(20f, 0f, 60f);
            carObject.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            yield return DriveForward(4);

            Assert.IsFalse(manager.HasRoadHeading);
            Assert.AreEqual(30f, view.RotationDegrees, Tolerance);
        }

        [UnityTest]
        public IEnumerator HeadingUp_OffRoad_SmallNoseChange_IgnoredByDeadZone()
        {
            followCar.SetRotationMode(MinimapRotationMode.HeadingUp);
            followCar.SetNoseDeadZoneDegrees(3f);
            carObject.transform.position = new Vector3(20f, 0f, 60f);
            carObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            yield return DriveForward(4);
            Assert.IsFalse(manager.HasRoadHeading);
            Assert.AreEqual(0f, view.RotationDegrees, Tolerance);

            carObject.transform.rotation = Quaternion.Euler(0f, 2f, 0f);
            yield return DriveForward(3);
            Assert.AreEqual(0f, view.RotationDegrees, Tolerance);

            carObject.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            yield return DriveForward(3);
            Assert.AreEqual(10f, view.RotationDegrees, Tolerance);
        }

        [UnityTest]
        public IEnumerator HeadingUp_LargeHeadingChange_UsesTurnSmoothing()
        {
            followCar.SetRotationMode(MinimapRotationMode.HeadingUp);
            carObject.transform.position = new Vector3(20f, 0f, 60f);
            carObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            yield return DriveForward(4);
            Assert.AreEqual(0f, view.RotationDegrees, Tolerance);

            followCar.SetTurnSmoothing(0.8f);
            followCar.enabled = false;
            carObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return DriveForward(1);
            Assert.AreEqual(0f, view.RotationDegrees, Tolerance);

            followCar.UpdateFollowCarVisuals(0.1f);

            Assert.Greater(view.RotationDegrees, 0.5f);
            Assert.Less(view.RotationDegrees, 89f);
        }

        [UnityTest]
        public IEnumerator NorthUp_RotationZero()
        {
            followCar.SetRotationMode(MinimapRotationMode.NorthUp);

            for (int i = 0; i < 3; i++)
            {
                carObject.transform.position += new Vector3(2f, 0f, 0f);
                yield return null;
            }

            Assert.AreEqual(0f, view.RotationDegrees, Tolerance);
        }

        [UnityTest]
        public IEnumerator NearMapEdge_CenterClamped()
        {
            followCar.SetRotationMode(MinimapRotationMode.NorthUp);
            carObject.transform.position = new Vector3(-30f, 0f, 0f);

            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(20f, manager.CarMapPosition.x, Tolerance);
            Assert.AreEqual(50f, view.CenterMap.x, Tolerance);
            Assert.AreEqual(100f, view.CenterMap.y, Tolerance);

            Vector2 carLocal = view.MapToViewport(manager.CarMapPosition);
            Assert.AreEqual(-60f, carLocal.x, 0.5f);
            Assert.AreEqual(0f, carLocal.y, 0.5f);
        }

        private IEnumerator DriveForward(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                carObject.transform.position += carObject.transform.forward * 2f;
                yield return null;
            }
        }

        private void AssertNosePointsUp()
        {
            Vector2 carMap = manager.CarMapPosition;
            Vector2 noseMap = carMap + new Vector2(1f, 0f);
            Vector2 direction = (view.MapToViewport(noseMap) - view.MapToViewport(carMap)).normalized;

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(1f, direction.y, Tolerance);
        }
    }
}
