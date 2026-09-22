using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;
using Gley.NavigationSystem.Tests;

namespace Gley.NavigationSystem.Dev
{
    public class TestMapBuilder
    {
        public const string DefaultFolder = "Assets/NavigationData/DevTestMap";
        public const string DefaultName = "DevTestMap";
        public const float DefaultRectangleSizeMeters = 200f;
        public const int DefaultTestRoadCount = 12;
        public const int DefaultTestRoadSeed = 1;

        [MenuItem("Tools/Gley/Navigation Dev/Create Test Map")]
        private static void CreateTestMapMenuItem()
        {
            new TestMapBuilder().CreateTestMap();
        }

        [MenuItem("Tools/Gley/Navigation Dev/Add Test Roads To Map")]
        private static void AddTestRoadsMenuItem()
        {
            new TestMapBuilder().AddTestRoadsToTestMap();
        }

        [MenuItem("Tools/Gley/Navigation Dev/Add Sandbox Roads To Map")]
        private static void AddSandboxRoadsMenuItem()
        {
            new TestMapBuilder().AddSandboxRoadsToTestMap();
        }

        public NavigationMap CreateTestMap()
        {
            NavigationAssetLocator locator = new NavigationAssetLocator();
            NavigationMapAssets assets = locator.CreateMapAssets(DefaultFolder, DefaultName);

            assets.MapAsset.SetRectangleSize(new Vector2(DefaultRectangleSizeMeters, DefaultRectangleSizeMeters));

            NavigationMap map = CreateMapObject(assets.MapAsset);

            EditorUtility.SetDirty(assets.MapAsset);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = map.gameObject;
            return map;
        }

        public NavigationMap CreateMapObject(MapData data)
        {
            GameObject mapObject = new GameObject("NavigationMap");
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            AssignMapData(map, data);

            float unitsPerMeter = new NavigationAssetLocator().FindOrCreateSettings().UnitsPerMeter;
            new MapRectangleSync().SnapObjectToAsset(mapObject.transform, data, unitsPerMeter);
            return map;
        }

        public RoadNetworkAuthoring AddTestRoadsToTestMap()
        {
            string path = DefaultFolder + "/" + DefaultName + "_RoadsAuthoring.asset";
            RoadNetworkAuthoring authoring = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
            if (authoring == null)
            {
                CreateTestMap();
                authoring = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
            }

            TestCityGenerator generator = new TestCityGenerator();
            RoadNetworkBuildInput input = generator.Generate(DefaultTestRoadCount, DefaultTestRoadSeed);

            AuthoringFromBuildInput converter = new AuthoringFromBuildInput();
            converter.Fill(authoring, input);
            ForceSomeRoadsOneWay(authoring);
            authoring.MarkChanged();

            EditorUtility.SetDirty(authoring);
            AssetDatabase.SaveAssets();
            return authoring;
        }

        public RoadNetworkAuthoring AddSandboxRoadsToTestMap()
        {
            string path = DefaultFolder + "/" + DefaultName + "_RoadsAuthoring.asset";
            RoadNetworkAuthoring authoring = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
            if (authoring == null)
            {
                CreateTestMap();
                authoring = AssetDatabase.LoadAssetAtPath<RoadNetworkAuthoring>(path);
            }

            SandboxRoadNetworkGenerator generator = new SandboxRoadNetworkGenerator();
            RoadNetworkBuildInput input = generator.Generate();

            authoring.Roads.Clear();
            authoring.Intersections.Clear();

            AuthoringFromBuildInput converter = new AuthoringFromBuildInput();
            converter.Fill(authoring, input);
            authoring.MarkChanged();

            ResizeMapToSandboxGrid(authoring.MapAsset);
            EnsureSettingsAssigned(authoring);

            EditorUtility.SetDirty(authoring);
            AssetDatabase.SaveAssets();

            new RoadBaker().Bake(authoring);
            return authoring;
        }

        private void AssignMapData(NavigationMap map, MapData data)
        {
            SerializedObject serializedObject = new SerializedObject(map);
            serializedObject.FindProperty("mapData").objectReferenceValue = data;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ForceSomeRoadsOneWay(RoadNetworkAuthoring authoring)
        {
            for (int i = 0; i < authoring.Roads.Count; i += 3)
            {
                authoring.Roads[i].SetOneWay(true);
            }
        }

        private void ResizeMapToSandboxGrid(MapData data)
        {
            if (data == null)
            {
                return;
            }

            float sizeMeters = SandboxSceneBuilder.GridBlockCount * SandboxSceneBuilder.BlockSizeMeters;
            data.SetRectangleCenter(Vector3.zero);
            data.SetRectangleSize(new Vector2(sizeMeters, sizeMeters));

            float unitsPerMeter = new NavigationAssetLocator().FindOrCreateSettings().UnitsPerMeter;
            new MapRectangleSync().SnapSceneObjectsToAsset(data, unitsPerMeter, "Resize Map Rectangle");
            EditorUtility.SetDirty(data);
        }

        private void EnsureSettingsAssigned(RoadNetworkAuthoring authoring)
        {
            if (authoring.Settings != null)
            {
                return;
            }

            NavigationSettings settings = new NavigationAssetLocator().FindOrCreateSettings();
            SerializedObject serializedObject = new SerializedObject(authoring);
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
