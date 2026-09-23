using Gley.Common;
using Gley.NavigationSystem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Dev
{
    public class PerfSceneBuilder
    {
        public const string ScenePath = "Assets/Tests/NavigationSystem/Dev/Scenes/PerfScene.unity";
        public const string PrefabFolder = "Assets/Gley/DrivingNavigationSystem/Prefabs";
        public const int StaticMarkerCount = 100;
        public const int MovingMarkerCount = 20;
        public const int MarkerSeed = 1;
        public const float MarkerAreaHalfSizeMeters = 600f;
        public const float MovingMarkerMinRadiusMeters = 30f;
        public const float MovingMarkerMaxRadiusMeters = 150f;
        public const float MovingMarkerMinDegreesPerSecond = 10f;
        public const float MovingMarkerMaxDegreesPerSecond = 40f;

        [MenuItem("Tools/Gley/Navigation Dev/Create Perf Scene")]
        private static void CreatePerfSceneMenuItem()
        {
            new PerfSceneBuilder().CreatePerfScene();
        }

        public void CreatePerfScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            NavigationSettings settings = new NavigationAssetLocator().FindOrCreateSettings();
            NavigationMap map = CreateOrReuseMap(settings);
            if (map == null || map.MapData == null || map.MapData.RoadNetwork == null || map.MapData.RoadNetwork.RoadCount == 0)
            {
                CustomLogger.LogError("PerfSceneBuilder: the 5k map has no baked roads. Run Tools > Gley > Navigation Dev > Generate 5k Road Map first.");
                return;
            }

            float unitsPerMeter = settings.UnitsPerMeter;
            RoadNetworkData network = map.MapData.RoadNetwork;
            Vector3 startTrue = FindCornerIntersection(network, false);
            Vector3 destinationTrue = FindCornerIntersection(network, true);

            Transform car = CreateCar(startTrue * unitsPerMeter);
            Transform destination = CreateDestination(destinationTrue * unitsPerMeter);

            NavigationManager manager = CreateManager(settings, car, map);
            DevAutoDriver driver = car.gameObject.AddComponent<DevAutoDriver>();
            driver.Configure(manager, destination);

            GameObject minimapRoot;
            MapViewInteractive fullMap = CreateUi(out minimapRoot);
            GameObject markersRoot = new GameObject("PerfMarkers");
            GameObject staticMarkers = CreateChild(markersRoot, "StaticMarkers");
            GameObject movingMarkers = CreateChild(markersRoot, "MovingMarkers");
            CreateMarkers(manager, startTrue, unitsPerMeter, staticMarkers.transform, movingMarkers.transform);

            GameObject perfTools = new GameObject("PerfTools");
            DevPerfPhaseCycler cycler = perfTools.AddComponent<DevPerfPhaseCycler>();
            cycler.Configure(minimapRoot, fullMap, staticMarkers, movingMarkers);
            DevPerfOverlay overlay = perfTools.AddComponent<DevPerfOverlay>();
            overlay.Configure(manager, fullMap, cycler);

            Scene scene = SceneManager.GetActiveScene();
            EnsureFolder(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = manager.gameObject;
        }

        private NavigationMap CreateOrReuseMap(NavigationSettings settings)
        {
            LargeNetworkGenerator generator = new LargeNetworkGenerator();
            string mapPath = LargeNetworkGenerator.DefaultFolder + "/" + LargeNetworkGenerator.DefaultName + "_Map.asset";
            MapData existing = AssetDatabase.LoadAssetAtPath<MapData>(mapPath);
            if (existing != null)
            {
                return generator.CreateMapObject(existing, settings.UnitsPerMeter);
            }

            generator.Generate();
            return Object.FindAnyObjectByType<NavigationMap>();
        }

        private Vector3 FindCornerIntersection(RoadNetworkData network, bool farCorner)
        {
            Vector3 best = Vector3.zero;
            float bestScore = 0f;
            for (int i = 0; i < network.IntersectionCount; i++)
            {
                Vector3 position = network.GetIntersection(i).Position;
                float score = position.x + position.z;
                if (!farCorner)
                {
                    score = -score;
                }

                if (i == 0 || score > bestScore)
                {
                    best = position;
                    bestScore = score;
                }
            }

            return best;
        }

        private Transform CreateCar(Vector3 worldPosition)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "Car";
            Object.DestroyImmediate(car.GetComponent<Collider>());
            car.transform.position = worldPosition;
            car.transform.localScale = new Vector3(2f, 1.5f, 4.5f);
            return car.transform;
        }

        private Transform CreateDestination(Vector3 worldPosition)
        {
            GameObject destination = new GameObject("Destination");
            destination.transform.position = worldPosition;
            return destination.transform;
        }

        private NavigationManager CreateManager(NavigationSettings settings, Transform car, NavigationMap map)
        {
            GameObject managerObject = new GameObject("NavigationManager");
            NavigationManager manager = managerObject.AddComponent<NavigationManager>();

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("settings").objectReferenceValue = settings;
            serializedManager.FindProperty("car").objectReferenceValue = car;
            serializedManager.FindProperty("explicitMap").objectReferenceValue = map;
            serializedManager.FindProperty("formatter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DefaultNavigationFormatter>(PrefabFolder + "/DefaultFormatter.asset");
            serializedManager.FindProperty("playerMarkerPrefab").objectReferenceValue = LoadPrefab("PlayerMarker");
            serializedManager.FindProperty("destinationMarkerPrefab").objectReferenceValue = LoadPrefab("DestinationMarker");
            serializedManager.FindProperty("previewPinPrefab").objectReferenceValue = LoadPrefab("PreviewPin");
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private GameObject LoadPrefab(string prefabName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
        }

        private MapViewInteractive CreateUi(out GameObject minimapRoot)
        {
            minimapRoot = null;
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject minimapPrefab = LoadPrefab("NavigationMinimap");
            GameObject fullMapPrefab = LoadPrefab("NavigationFullMap");
            if (minimapPrefab == null || fullMapPrefab == null)
            {
                CustomLogger.LogError("PerfSceneBuilder: default prefabs are missing. Run Tools > Gley > Navigation Dev > Build Default Prefabs.");
                return null;
            }

            GameObject minimapInstance = (GameObject)PrefabUtility.InstantiatePrefab(minimapPrefab, canvasObject.transform);
            minimapRoot = minimapInstance;
            GameObject fullMapInstance = (GameObject)PrefabUtility.InstantiatePrefab(fullMapPrefab, canvasObject.transform);

            MinimapTapToOpen tapToOpen = minimapInstance.GetComponentInChildren<MinimapTapToOpen>(true);
            MapViewInteractive interactive = fullMapInstance.GetComponentInChildren<MapViewInteractive>(true);
            if (tapToOpen != null && interactive != null)
            {
                tapToOpen.SetFullMap(interactive);
            }
            return interactive;
        }

        private GameObject CreateChild(GameObject parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private void CreateMarkers(NavigationManager manager, Vector3 centerTrue, float unitsPerMeter, Transform staticParent, Transform movingParent)
        {
            GameObject markerPrefab = LoadPrefab("DefaultMarker");
            System.Random random = new System.Random(MarkerSeed);

            for (int i = 0; i < StaticMarkerCount; i++)
            {
                MapMarker marker = CreateMarker(staticParent, "StaticMarker" + i, RandomPosition(random, centerTrue) * unitsPerMeter, manager, markerPrefab);
                marker.SetIsStatic(true);
            }

            for (int i = 0; i < MovingMarkerCount; i++)
            {
                MapMarker marker = CreateMarker(movingParent, "MovingMarker" + i, RandomPosition(random, centerTrue) * unitsPerMeter, manager, markerPrefab);
                marker.SetIsStatic(false);
                marker.SetShowOffScreenArrow(true);

                float radius = Lerp(random, MovingMarkerMinRadiusMeters, MovingMarkerMaxRadiusMeters) * unitsPerMeter;
                float degreesPerSecond = Lerp(random, MovingMarkerMinDegreesPerSecond, MovingMarkerMaxDegreesPerSecond);
                float startAngle = Lerp(random, 0f, 360f);
                DevMarkerMover mover = marker.gameObject.AddComponent<DevMarkerMover>();
                mover.Configure(radius, degreesPerSecond, startAngle);
            }
        }

        private MapMarker CreateMarker(Transform parent, string markerName, Vector3 worldPosition, NavigationManager manager, GameObject markerPrefab)
        {
            GameObject markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.position = worldPosition;

            MapMarker marker = markerObject.AddComponent<MapMarker>();
            marker.SetManager(manager);
            marker.SetPrefab(markerPrefab);
            return marker;
        }

        private Vector3 RandomPosition(System.Random random, Vector3 centerTrue)
        {
            float x = centerTrue.x + Lerp(random, -MarkerAreaHalfSizeMeters, MarkerAreaHalfSizeMeters);
            float z = centerTrue.z + Lerp(random, -MarkerAreaHalfSizeMeters, MarkerAreaHalfSizeMeters);
            return new Vector3(x, centerTrue.y, z);
        }

        private float Lerp(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private void EnsureFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
