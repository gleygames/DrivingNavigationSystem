using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationManagerApiTests
    {
        private const float Tolerance = 1f;
        private const string MapChangedEvent = "MapChanged";
        private const string CarChangedEvent = "CarChanged";
        private const string PreviewReadyEvent = "PreviewReady";
        private const string PreviewFailedEvent = "PreviewFailed";
        private const string PreviewCanceledEvent = "PreviewCanceled";
        private const string NavigationStartedEvent = "NavigationStarted";
        private const string ReroutedEvent = "Rerouted";
        private const string RouteFailedEvent = "RouteFailed";
        private const string ArrivedEvent = "Arrived";
        private const string NavigationStoppedEvent = "NavigationStopped";
        private const string OffRoadEvent = "OffRoad";
        private const string BackOnRoadEvent = "BackOnRoad";
        private const string OutsideMapEvent = "OutsideMap";
        private const string BackInsideMapEvent = "BackInsideMap";

        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private readonly List<MapData> mapAssets = new List<MapData>();
        private readonly List<string> eventLog = new List<string>();
        private readonly List<int> eventFrames = new List<int>();
        private readonly List<RerouteReason> rerouteReasons = new List<RerouteReason>();

        private NavigationSettings settings;
        private RoadNetworkData network;
        private NavigationManager manager;
        private Transform car;
        private NavigationMap lastMap;
        private Transform lastCar;
        private Route lastPreviewRoute;
        private Route lastStartedRoute;
        private Route requestedRoute;
        private FailureReason lastPreviewFailure;
        private FailureReason lastRouteFailure;
        private StopReason lastStopReason;
        private Vector3 nextStop;
        private bool hadActiveRouteAfterQueuedCall;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            eventLog.Clear();
            eventFrames.Clear();
            rerouteReasons.Clear();
            lastMap = null;
            lastCar = null;
            lastPreviewRoute = null;
            lastStartedRoute = null;
            requestedRoute = null;
            lastPreviewFailure = FailureReason.None;
            lastRouteFailure = FailureReason.None;
            lastStopReason = StopReason.StopCalled;
            hadActiveRouteAfterQueuedCall = false;

            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Line(3, 100f));

            car = CreateCar("TestCar", new Vector3(20f, 0f, 0f));
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

            DestroyNetwork();
            Object.DestroyImmediate(settings);
        }

        [UnityTest]
        public IEnumerator PreviewDestination_NoMap_PreviewFailedNoMap()
        {
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));

            Assert.AreEqual(1, CountEvents(PreviewFailedEvent));
            Assert.AreEqual(FailureReason.NoMap, lastPreviewFailure);
            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator PreviewDestination_NoCar_PreviewFailedNoCar()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.SetCar(null);
            manager.PreviewDestination(new Vector3(280f, 0f, 0f));

            Assert.AreEqual(1, CountEvents(PreviewFailedEvent));
            Assert.AreEqual(FailureReason.NoCar, lastPreviewFailure);
        }

        [UnityTest]
        public IEnumerator PreviewDestination_Failure_ActiveRouteUntouched()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            Route active = manager.ActiveRoute;
            float remaining = manager.RemainingDistance;

            manager.PreviewDestination(new Vector3(150f, 0f, 500f));

            Assert.AreEqual(1, CountEvents(PreviewFailedEvent));
            Assert.AreEqual(FailureReason.NoRoadNearDestination, lastPreviewFailure);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreSame(active, manager.ActiveRoute);
            Assert.AreEqual(remaining, manager.RemainingDistance, Tolerance);
            Assert.AreEqual(0, CountEvents(RouteFailedEvent));
        }

        [UnityTest]
        public IEnumerator PreviewDestination_Success_PreviewReady()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));

            Assert.AreEqual(1, CountEvents(PreviewReadyEvent));
            Assert.IsTrue(manager.HasPreview);
            Assert.IsNotNull(manager.PreviewRoute);
            Assert.AreSame(manager.PreviewRoute, lastPreviewRoute);
            Assert.IsTrue(lastPreviewRoute.Success);
            Assert.AreEqual(260f, lastPreviewRoute.Length, Tolerance);
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator PreviewDestination_NewPreview_ReplacesOld()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));

            Assert.AreEqual(2, CountEvents(PreviewReadyEvent));
            Assert.IsTrue(manager.HasPreview);
            Assert.AreEqual(130f, manager.PreviewRoute.Length, Tolerance);
            Assert.AreEqual(150f, manager.PreviewRoute.Destination.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator PreviewWhileDriving_RoadChanged_PreviewRecomputed()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));
            Assert.AreEqual(1, CountEvents(PreviewReadyEvent));

            yield return DriveCar(new Vector3(5f, 0f, 0f), 20);

            Assert.GreaterOrEqual(CountEvents(PreviewReadyEvent), 2);
            Assert.AreEqual(0, CountEvents(PreviewFailedEvent));
            Assert.Less(manager.PreviewRoute.Length, 200f);
        }

        [UnityTest]
        public IEnumerator ConfirmPreview_Success_BecomesActiveRoute()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));
            manager.ConfirmPreview();

            Assert.AreEqual(1, CountEvents(NavigationStartedEvent));
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.IsFalse(manager.HasPreview);
            Assert.IsNull(manager.PreviewRoute);
            Assert.AreSame(manager.ActiveRoute, lastStartedRoute);
            Assert.AreEqual(260f, manager.RemainingDistance, Tolerance);
            Assert.AreEqual(0, CountEvents(PreviewCanceledEvent));
        }

        [UnityTest]
        public IEnumerator ConfirmPreview_Failure_RouteFailedNavigationKept()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            Route active = manager.ActiveRoute;
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));

            car.position = new Vector3(150f, 0f, 500f);
            manager.ConfirmPreview();

            Assert.AreEqual(1, CountEvents(RouteFailedEvent));
            Assert.AreEqual(FailureReason.NoRoadNearStart, lastRouteFailure);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreSame(active, manager.ActiveRoute);
            Assert.AreEqual(1, CountEvents(NavigationStartedEvent));
        }

        [UnityTest]
        public IEnumerator CancelPreview_WithPreview_PreviewCanceled()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.CancelPreview();
            Assert.AreEqual(0, CountEvents(PreviewCanceledEvent));

            manager.PreviewDestination(new Vector3(280f, 0f, 0f));
            manager.CancelPreview();

            Assert.AreEqual(1, CountEvents(PreviewCanceledEvent));
            Assert.IsFalse(manager.HasPreview);
            Assert.IsNull(manager.PreviewRoute);
        }

        [UnityTest]
        public IEnumerator StartNavigation_Success_NavigationStarted()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));

            Assert.AreEqual(1, CountEvents(NavigationStartedEvent));
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreSame(manager.ActiveRoute, lastStartedRoute);
            Assert.AreEqual(260f, manager.RemainingDistance, Tolerance);
            Assert.Greater(manager.Eta, 0f);
            Assert.AreEqual(0f, manager.TrimDistance, Tolerance);
        }

        [UnityTest]
        public IEnumerator StartNavigation_Failure_RouteFailedCurrentKept()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            Route active = manager.ActiveRoute;

            manager.StartNavigation(new Vector3(150f, 0f, 500f));

            Assert.AreEqual(1, CountEvents(RouteFailedEvent));
            Assert.AreEqual(FailureReason.NoRoadNearDestination, lastRouteFailure);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreSame(active, manager.ActiveRoute);
            Assert.AreEqual(260f, manager.RemainingDistance, Tolerance);
        }

        [UnityTest]
        public IEnumerator StopNavigation_Active_NavigationStoppedStopCalled()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StopNavigation();
            Assert.AreEqual(0, CountEvents(NavigationStoppedEvent));

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            manager.StopNavigation();

            Assert.AreEqual(1, CountEvents(NavigationStoppedEvent));
            Assert.AreEqual(StopReason.StopCalled, lastStopReason);
            Assert.IsFalse(manager.HasActiveRoute);
            Assert.IsNull(manager.ActiveRoute);
            Assert.AreEqual(0f, manager.RemainingDistance);
        }

        [UnityTest]
        public IEnumerator Reroute_Failure_RouteFailedAndNavigationStops()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            car.position = new Vector3(150f, 0f, 500f);
            yield return null;

            Assert.AreEqual(1, CountEvents(RouteFailedEvent));
            Assert.AreEqual(FailureReason.NoRoadNearStart, lastRouteFailure);
            Assert.IsFalse(manager.HasActiveRoute);
            Assert.AreEqual(0, CountEvents(ReroutedEvent));
            Assert.AreEqual(0, CountEvents(NavigationStoppedEvent));
        }

        [UnityTest]
        public IEnumerator TeleportOnRoute_JumpsProgress_NoEvent()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            eventLog.Clear();

            car.position = new Vector3(180f, 0f, 0f);
            yield return null;

            Assert.IsTrue(manager.CarTeleported);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreEqual(0, CountEvents(ReroutedEvent));
            Assert.AreEqual(0, CountEvents(RouteFailedEvent));
            Assert.AreEqual(0, CountEvents(ArrivedEvent));
            Assert.AreEqual(100f, manager.RemainingDistance, Tolerance);
            Assert.AreEqual(160f, manager.TrimDistance, Tolerance);
        }

        [UnityTest]
        public IEnumerator Arrival_ClearsRoute_ArrivedFires()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(60f, 0f, 0f));
            yield return DriveCar(new Vector3(5f, 0f, 0f), 8);

            Assert.AreEqual(1, CountEvents(ArrivedEvent));
            Assert.IsFalse(manager.HasActiveRoute);
            Assert.IsNull(manager.ActiveRoute);
            Assert.AreEqual(0, CountEvents(NavigationStoppedEvent));
        }

        [UnityTest]
        public IEnumerator StartNavigation_ArrivedImmediately_ArrivedWithoutNavigationStarted()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(25f, 0f, 0f));

            Assert.AreEqual(1, CountEvents(ArrivedEvent));
            Assert.AreEqual(0, CountEvents(NavigationStartedEvent));
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator SetMap_DuringNavigationAndPreview_StopsCancelsThenMapChanged()
        {
            NavigationMap first = CreateMap("MapA");
            NavigationMap second = CreateMap("MapB");
            CreateManager();
            manager.SetExplicitMap(first);
            yield return WaitFrames(2);

            Assert.AreSame(first, manager.ActiveMap);
            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));
            eventLog.Clear();

            manager.SetMap(second);

            Assert.AreSame(second, manager.ActiveMap);
            Assert.AreSame(second, lastMap);
            Assert.AreEqual(StopReason.MapChanged, lastStopReason);
            Assert.AreEqual(3, eventLog.Count);
            Assert.AreEqual(NavigationStoppedEvent, eventLog[0]);
            Assert.AreEqual(PreviewCanceledEvent, eventLog[1]);
            Assert.AreEqual(MapChangedEvent, eventLog[2]);
            Assert.IsFalse(manager.HasActiveRoute);
            Assert.IsFalse(manager.HasPreview);
        }

        [UnityTest]
        public IEnumerator SetCar_DuringNavigation_ReroutedCarChangedThenCarChanged()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));
            eventLog.Clear();

            Transform newCar = CreateCar("NewCar", new Vector3(120f, 0f, 0f));
            manager.SetCar(newCar);

            Assert.AreEqual(3, eventLog.Count);
            Assert.AreEqual(ReroutedEvent, eventLog[0]);
            Assert.AreEqual(PreviewReadyEvent, eventLog[1]);
            Assert.AreEqual(CarChangedEvent, eventLog[2]);
            Assert.AreEqual(RerouteReason.CarChanged, rerouteReasons[0]);
            Assert.AreSame(newCar, lastCar);
            Assert.AreSame(newCar, manager.Car);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreEqual(160f, manager.RemainingDistance, Tolerance);
            Assert.AreEqual(30f, manager.PreviewRoute.Length, Tolerance);
        }

        [UnityTest]
        public IEnumerator SetCar_RerouteFails_RouteFailedAndStops()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            eventLog.Clear();

            Transform farCar = CreateCar("FarCar", new Vector3(150f, 0f, 500f));
            manager.SetCar(farCar);

            Assert.AreEqual(2, eventLog.Count);
            Assert.AreEqual(RouteFailedEvent, eventLog[0]);
            Assert.AreEqual(CarChangedEvent, eventLog[1]);
            Assert.AreEqual(FailureReason.NoRoadNearStart, lastRouteFailure);
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator SetCarNull_DuringNavigation_NavigationStoppedCarRemoved()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            eventLog.Clear();

            manager.SetCar(null);

            Assert.AreEqual(2, eventLog.Count);
            Assert.AreEqual(NavigationStoppedEvent, eventLog[0]);
            Assert.AreEqual(CarChangedEvent, eventLog[1]);
            Assert.AreEqual(StopReason.CarRemoved, lastStopReason);
            Assert.IsNull(lastCar);
            Assert.IsNull(manager.Car);
            Assert.IsFalse(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator PreferenceSetter_DuringNavigation_RecomputedAtEndOfFrame()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            manager.PreviewDestination(new Vector3(150f, 0f, 0f));
            eventLog.Clear();

            manager.SetRoadTypePreference(1, RoadTypePreference.Avoid);

            Assert.AreEqual(0, eventLog.Count);

            yield return null;

            Assert.AreEqual(1, CountEvents(ReroutedEvent));
            Assert.AreEqual(RerouteReason.PreferencesChanged, rerouteReasons[0]);
            Assert.AreEqual(1, CountEvents(PreviewReadyEvent));
            Assert.IsTrue(manager.HasActiveRoute);
        }

        [UnityTest]
        public IEnumerator PreferenceSetter_NoActiveRoute_JustStores()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);
            eventLog.Clear();

            manager.SetRouteMode(RouteMode.Fastest);
            manager.SetUTurnRule(UTurnRule.Anywhere);
            yield return WaitFrames(2);

            Assert.AreEqual(0, eventLog.Count);
        }

        [UnityTest]
        public IEnumerator PreferencesChangedTwiceInOneFrame_OneReroute()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            eventLog.Clear();

            manager.SetRouteMode(RouteMode.Fastest);
            manager.SetUTurnRule(UTurnRule.Anywhere);
            yield return WaitFrames(2);

            Assert.AreEqual(1, CountEvents(ReroutedEvent));
            Assert.AreEqual(RerouteReason.PreferencesChanged, rerouteReasons[0]);
        }

        [UnityTest]
        public IEnumerator OffRoadTransitions_FireOncePerTransition()
        {
            CreateMap("Map");
            CreateManager();
            car.position = new Vector3(50f, 0f, 0f);
            yield return WaitFrames(2);

            yield return DriveCar(new Vector3(0f, 0f, 5f), 6);
            yield return WaitFrames(3);

            Assert.IsTrue(manager.IsOffRoad);
            Assert.AreEqual(1, CountEvents(OffRoadEvent));
            Assert.AreEqual(0, CountEvents(BackOnRoadEvent));

            yield return DriveCar(new Vector3(0f, 0f, -5f), 6);
            yield return WaitFrames(3);

            Assert.IsFalse(manager.IsOffRoad);
            Assert.AreEqual(1, CountEvents(OffRoadEvent));
            Assert.AreEqual(1, CountEvents(BackOnRoadEvent));
        }

        [UnityTest]
        public IEnumerator OutsideMapTransitions_FireOncePerTransition()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            car.position = new Vector3(400f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.IsTrue(manager.IsOutsideMap);
            Assert.AreEqual(1, CountEvents(OutsideMapEvent));

            car.position = new Vector3(20f, 0f, 0f);
            yield return WaitFrames(3);

            Assert.IsFalse(manager.IsOutsideMap);
            Assert.AreEqual(1, CountEvents(OutsideMapEvent));
            Assert.AreEqual(1, CountEvents(BackInsideMapEvent));
        }

        [UnityTest]
        public IEnumerator EventHandler_CallsStartNavigationInsideArrived_AppliedAfterDispatch()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            nextStop = new Vector3(280f, 0f, 0f);
            manager.Arrived += StartNextStopOnArrived;
            manager.StartNavigation(new Vector3(60f, 0f, 0f));
            eventLog.Clear();
            eventFrames.Clear();

            yield return DriveCar(new Vector3(5f, 0f, 0f), 8);
            manager.Arrived -= StartNextStopOnArrived;

            Assert.AreEqual(2, eventLog.Count);
            Assert.AreEqual(ArrivedEvent, eventLog[0]);
            Assert.AreEqual(NavigationStartedEvent, eventLog[1]);
            Assert.AreEqual(eventFrames[0], eventFrames[1]);
            Assert.IsFalse(hadActiveRouteAfterQueuedCall);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreEqual(280f, manager.ActiveRoute.Destination.x, Tolerance);
        }

        [UnityTest]
        public IEnumerator EventHandlerLoop_StopsAfter3Rounds_LogsError()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            nextStop = new Vector3(280f, 0f, 0f);
            manager.NavigationStarted += RestartOnNavigationStarted;
            LogAssert.Expect(LogType.Error, new Regex("rounds"));

            manager.StartNavigation(nextStop);
            manager.NavigationStarted -= RestartOnNavigationStarted;

            Assert.AreEqual(4, CountEvents(NavigationStartedEvent));
            Assert.IsTrue(manager.HasActiveRoute);

            yield return null;

            Assert.AreEqual(4, CountEvents(NavigationStartedEvent));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RequestRoute_DoesNotChangeActiveRoute()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));
            Route active = manager.ActiveRoute;
            float remaining = manager.RemainingDistance;
            eventLog.Clear();

            NavigationRouteRequest request = new NavigationRouteRequest(new Vector3(120f, 0f, 0f), new Vector3(180f, 0f, 0f));
            manager.RequestRoute(request, StoreRequestedRoute);

            Assert.IsNotNull(requestedRoute);
            Assert.IsTrue(requestedRoute.Success);
            Assert.AreNotSame(active, requestedRoute);
            Assert.AreEqual(60f, requestedRoute.Length, Tolerance);
            Assert.AreSame(active, manager.ActiveRoute);
            Assert.AreEqual(remaining, manager.RemainingDistance, Tolerance);
            Assert.AreEqual(0, eventLog.Count);
        }

        [UnityTest]
        public IEnumerator RequestRoute_GetPoints_AreWorldPositions()
        {
            NavigationMap map = CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            Vector3 shift = new Vector3(1000f, 0f, 0f);
            map.transform.position += shift;
            car.position += shift;
            yield return null;

            NavigationRouteRequest request = new NavigationRouteRequest(new Vector3(1020f, 0f, 0f), new Vector3(1250f, 0f, 0f));
            manager.RequestRoute(request, StoreRequestedRoute);

            Assert.IsTrue(requestedRoute.Success);
            List<Vector3> points = new List<Vector3>();
            requestedRoute.GetPoints(points);

            Assert.GreaterOrEqual(points.Count, 2);
            Assert.AreEqual(1020f, points[0].x, Tolerance);
            Assert.AreEqual(1250f, points[points.Count - 1].x, Tolerance);
        }

        [UnityTest]
        public IEnumerator RequestRoute_NoMap_CallbackWithNoMapFailure()
        {
            CreateManager();
            yield return WaitFrames(2);

            NavigationRouteRequest request = new NavigationRouteRequest(new Vector3(20f, 0f, 0f), new Vector3(280f, 0f, 0f));
            manager.RequestRoute(request, StoreRequestedRoute);

            Assert.IsNotNull(requestedRoute);
            Assert.IsFalse(requestedRoute.Success);
            Assert.AreEqual(FailureReason.NoMap, requestedRoute.Failure);
        }

        [UnityTest]
        public IEnumerator DriveToDestination_ArrivedFires()
        {
            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(280f, 0f, 0f));

            for (int i = 0; i < 60; i++)
            {
                if (CountEvents(ArrivedEvent) > 0)
                {
                    break;
                }
                car.position += new Vector3(5f, 0f, 0f);
                yield return null;
            }

            Assert.AreEqual(1, CountEvents(ArrivedEvent));
            Assert.AreEqual(0, CountEvents(ReroutedEvent));
            Assert.IsFalse(manager.HasActiveRoute);
            Assert.GreaterOrEqual(car.position.x, 265f);
        }

        [UnityTest]
        public IEnumerator WrongTurn_ReroutedWithReason()
        {
            DestroyNetwork();
            TestNetworks networks = new TestNetworks();
            network = networks.BuildNetwork(networks.Grid(2, 2, 100f));

            CreateMap("Map");
            CreateManager();
            yield return WaitFrames(2);

            manager.StartNavigation(new Vector3(200f, 0f, 50f));
            Assert.IsTrue(manager.HasActiveRoute);

            yield return DriveCar(new Vector3(5f, 0f, 0f), 17);
            Assert.AreEqual(0, CountEvents(ReroutedEvent));

            yield return DriveCar(new Vector3(0f, 0f, 5f), 8);

            Assert.GreaterOrEqual(CountEvents(ReroutedEvent), 1);
            Assert.AreEqual(RerouteReason.WrongTurn, rerouteReasons[0]);
            Assert.IsTrue(manager.HasActiveRoute);
            Assert.AreEqual(0, CountEvents(RouteFailedEvent));
        }

        private void CreateManager()
        {
            GameObject managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetCarReference(car, 0f);
            Subscribe();
        }

        private void Subscribe()
        {
            manager.MapChanged += OnMapChanged;
            manager.CarChanged += OnCarChanged;
            manager.PreviewReady += OnPreviewReady;
            manager.PreviewFailed += OnPreviewFailed;
            manager.PreviewCanceled += OnPreviewCanceled;
            manager.NavigationStarted += OnNavigationStarted;
            manager.Rerouted += OnRerouted;
            manager.RouteFailed += OnRouteFailed;
            manager.Arrived += OnArrived;
            manager.NavigationStopped += OnNavigationStopped;
            manager.OffRoad += OnOffRoad;
            manager.BackOnRoad += OnBackOnRoad;
            manager.OutsideMap += OnOutsideMap;
            manager.BackInsideMap += OnBackInsideMap;
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

        private int CountEvents(string name)
        {
            int count = 0;
            for (int i = 0; i < eventLog.Count; i++)
            {
                if (eventLog[i] == name)
                {
                    count++;
                }
            }
            return count;
        }

        private void Record(string name)
        {
            eventLog.Add(name);
            eventFrames.Add(Time.frameCount);
        }

        private void OnMapChanged(NavigationMap map)
        {
            lastMap = map;
            Record(MapChangedEvent);
        }

        private void OnCarChanged(Transform value)
        {
            lastCar = value;
            Record(CarChangedEvent);
        }

        private void OnPreviewReady(Route route, MapMarker marker)
        {
            lastPreviewRoute = route;
            Record(PreviewReadyEvent);
        }

        private void OnPreviewFailed(FailureReason reason)
        {
            lastPreviewFailure = reason;
            Record(PreviewFailedEvent);
        }

        private void OnPreviewCanceled()
        {
            Record(PreviewCanceledEvent);
        }

        private void OnNavigationStarted(Route route)
        {
            lastStartedRoute = route;
            Record(NavigationStartedEvent);
        }

        private void OnRerouted(Route route, RerouteReason reason)
        {
            rerouteReasons.Add(reason);
            Record(ReroutedEvent);
        }

        private void OnRouteFailed(FailureReason reason)
        {
            lastRouteFailure = reason;
            Record(RouteFailedEvent);
        }

        private void OnArrived()
        {
            Record(ArrivedEvent);
        }

        private void OnNavigationStopped(StopReason reason)
        {
            lastStopReason = reason;
            Record(NavigationStoppedEvent);
        }

        private void OnOffRoad()
        {
            Record(OffRoadEvent);
        }

        private void OnBackOnRoad()
        {
            Record(BackOnRoadEvent);
        }

        private void OnOutsideMap()
        {
            Record(OutsideMapEvent);
        }

        private void OnBackInsideMap()
        {
            Record(BackInsideMapEvent);
        }

        private void StartNextStopOnArrived()
        {
            manager.StartNavigation(nextStop);
            hadActiveRouteAfterQueuedCall = manager.HasActiveRoute;
        }

        private void RestartOnNavigationStarted(Route route)
        {
            manager.StartNavigation(nextStop);
        }

        private void StoreRequestedRoute(Route route)
        {
            requestedRoute = route;
        }
    }
}
