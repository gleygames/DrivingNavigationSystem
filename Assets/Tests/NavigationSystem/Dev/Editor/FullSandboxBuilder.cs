using Gley.NavigationSystem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    public class FullSandboxBuilder
    {
        public const string CarObjectPath = "Sandbox/Car";
        public const float MinimapSizeCanvasUnits = 320f;
        public const float MinimapMarginCanvasUnits = 30f;

        [MenuItem("Tools/Gley/Navigation Dev/Create Full Sandbox (everything)", false, 0)]
        private static void CreateFullSandboxMenuItem()
        {
            new FullSandboxBuilder().CreateFullSandbox();
        }

        public void CreateFullSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            new SandboxSceneBuilder().CreateSandboxScene();
            Transform car = GameObject.Find(CarObjectPath).transform;

            TestMapBuilder mapBuilder = new TestMapBuilder();
            NavigationMap map = CreateOrReuseMap(mapBuilder);
            RoadNetworkAuthoring authoring = mapBuilder.AddSandboxRoadsToTestMap();

            NavigationSettings settings = new NavigationAssetLocator().FindOrCreateSettings();
            CaptureMapImage(authoring.MapAsset, settings.UnitsPerMeter, car.gameObject);

            NavigationManager manager = CreateManager(settings, car, map);
            MapViewFollowCar minimap = CreateUi(manager);
            DevNavigationTester tester = manager.gameObject.AddComponent<DevNavigationTester>();
            tester.Configure(manager, minimap);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = manager.gameObject;
        }

        private NavigationMap CreateOrReuseMap(TestMapBuilder mapBuilder)
        {
            string mapPath = TestMapBuilder.DefaultFolder + "/" + TestMapBuilder.DefaultName + "_Map.asset";
            MapData existing = AssetDatabase.LoadAssetAtPath<MapData>(mapPath);
            if (existing == null)
            {
                return mapBuilder.CreateTestMap();
            }

            return mapBuilder.CreateMapObject(existing);
        }

        private void CaptureMapImage(MapData data, float unitsPerMeter, GameObject car)
        {
            car.SetActive(false);
            try
            {
                new MapCaptureExecutor().Capture(data, new CaptureSettings(), unitsPerMeter);
            }
            finally
            {
                car.SetActive(true);
            }
        }

        private NavigationManager CreateManager(NavigationSettings settings, Transform car, NavigationMap map)
        {
            GameObject managerObject = new GameObject("NavigationManager");
            NavigationManager manager = managerObject.AddComponent<NavigationManager>();

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("settings").objectReferenceValue = settings;
            serializedManager.FindProperty("car").objectReferenceValue = car;
            serializedManager.FindProperty("explicitMap").objectReferenceValue = map;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private MapViewFollowCar CreateUi(NavigationManager manager)
        {
            GameObject canvasObject = new GameObject("DevCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject minimapObject = new GameObject("Minimap", typeof(RectTransform));
            minimapObject.transform.SetParent(canvasObject.transform, false);
            RectTransform minimapRect = minimapObject.GetComponent<RectTransform>();
            minimapRect.anchorMin = new Vector2(1f, 0f);
            minimapRect.anchorMax = new Vector2(1f, 0f);
            minimapRect.pivot = new Vector2(1f, 0f);
            minimapRect.anchoredPosition = new Vector2(-MinimapMarginCanvasUnits, MinimapMarginCanvasUnits);
            minimapRect.sizeDelta = new Vector2(MinimapSizeCanvasUnits, MinimapSizeCanvasUnits);
            minimapObject.AddComponent<RectMask2D>();

            MapView view = minimapObject.AddComponent<MapView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("manager").objectReferenceValue = manager;
            serializedView.FindProperty("showPreview").boolValue = false;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            MapViewFollowCar followCar = minimapObject.AddComponent<MapViewFollowCar>();
            SerializedObject serializedFollow = new SerializedObject(followCar);
            serializedFollow.FindProperty("round").boolValue = false;
            serializedFollow.ApplyModifiedPropertiesWithoutUndo();

            minimapObject.AddComponent<DevMinimapCarMarker>();

            GameObject fullMapObject = CreateFullMap(canvasObject, manager);

            MinimapTapToOpen tapToOpen = minimapObject.AddComponent<MinimapTapToOpen>();
            SerializedObject serializedTapToOpen = new SerializedObject(tapToOpen);
            serializedTapToOpen.FindProperty("fullMap").objectReferenceValue = fullMapObject.GetComponent<MapViewInteractive>();
            serializedTapToOpen.ApplyModifiedPropertiesWithoutUndo();

            return followCar;
        }

        private GameObject CreateFullMap(GameObject canvasObject, NavigationManager manager)
        {
            GameObject fullMapObject = new GameObject("FullMap", typeof(RectTransform));
            fullMapObject.transform.SetParent(canvasObject.transform, false);
            RectTransform fullMapRect = fullMapObject.GetComponent<RectTransform>();
            fullMapRect.anchorMin = Vector2.zero;
            fullMapRect.anchorMax = Vector2.one;
            fullMapRect.offsetMin = Vector2.zero;
            fullMapRect.offsetMax = Vector2.zero;

            MapView fullMapView = fullMapObject.AddComponent<MapView>();
            SerializedObject serializedFullMapView = new SerializedObject(fullMapView);
            serializedFullMapView.FindProperty("manager").objectReferenceValue = manager;
            serializedFullMapView.ApplyModifiedPropertiesWithoutUndo();

            fullMapObject.AddComponent<MapViewInteractive>();
            fullMapObject.AddComponent<PointerInputAdapter>();
            fullMapObject.SetActive(false);

            return fullMapObject;
        }
    }
}
