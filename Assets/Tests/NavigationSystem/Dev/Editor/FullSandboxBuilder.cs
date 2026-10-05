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
            DevUiInstaller installer = new DevUiInstaller();
            installer.AssignDefaultSettingsAssets(settings);
            GameObject canvasObject = CreateCanvasWithEventSystem();
            installer.InstallDefaultUi(canvasObject.transform);
            NavigationMinimap minimap = null;
            if (installer.Minimap != null)
            {
                minimap = installer.Minimap.GetComponent<NavigationMinimap>();
            }

            DevNavigationTester tester = manager.gameObject.AddComponent<DevNavigationTester>();
            tester.Configure(manager, minimap);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = manager.gameObject;
        }

        internal NavigationMap CreateOrReuseMap(TestMapBuilder mapBuilder)
        {
            string mapPath = TestMapBuilder.DefaultFolder + "/" + TestMapBuilder.DefaultName + "_Map.asset";
            MapData existing = AssetDatabase.LoadAssetAtPath<MapData>(mapPath);
            if (existing == null)
            {
                return mapBuilder.CreateTestMap();
            }

            return mapBuilder.CreateMapObject(existing);
        }

        internal void CaptureMapImage(MapData data, float unitsPerMeter, GameObject car)
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

        internal NavigationManager CreateManager(NavigationSettings settings, Transform car, NavigationMap map)
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

        internal GameObject CreateCanvasWithEventSystem()
        {
            GameObject canvasObject = new GameObject("DevCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return canvasObject;
        }
    }
}
