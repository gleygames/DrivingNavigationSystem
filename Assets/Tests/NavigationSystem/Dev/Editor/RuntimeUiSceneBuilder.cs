using Gley.NavigationSystem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.NavigationSystem.Dev
{
    public class RuntimeUiSceneBuilder
    {
        public const string ScenePath = "Assets/Tests/NavigationSystem/Dev/Scenes/RuntimeUiTest.unity";

        [MenuItem("Tools/Gley/Navigation Dev/Create Runtime UI Test Scene", false, 1)]
        private static void CreateRuntimeUiSceneMenuItem()
        {
            new RuntimeUiSceneBuilder().CreateRuntimeUiScene();
        }

        public void CreateRuntimeUiScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            new SandboxSceneBuilder().CreateSandboxScene();
            Transform car = GameObject.Find(FullSandboxBuilder.CarObjectPath).transform;

            FullSandboxBuilder sandboxBuilder = new FullSandboxBuilder();
            TestMapBuilder mapBuilder = new TestMapBuilder();
            NavigationMap map = sandboxBuilder.CreateOrReuseMap(mapBuilder);
            RoadNetworkAuthoring authoring = mapBuilder.AddSandboxRoadsToTestMap();

            NavigationSettings settings = new NavigationAssetLocator().FindOrCreateSettings();
            sandboxBuilder.CaptureMapImage(authoring.MapAsset, settings.UnitsPerMeter, car.gameObject);

            NavigationManager manager = sandboxBuilder.CreateManager(settings, car, map);
            DevUiInstaller installer = new DevUiInstaller();
            installer.AssignDefaultManagerAssets(manager);
            DevNavigationTester tester = manager.gameObject.AddComponent<DevNavigationTester>();
            tester.Configure(manager, null);
            sandboxBuilder.CreateCanvasWithEventSystem();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeObject = installer.LoadPrefab("NavigationMinimap");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}
