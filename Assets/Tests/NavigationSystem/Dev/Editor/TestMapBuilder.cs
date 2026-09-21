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

        public NavigationMap CreateTestMap()
        {
            NavigationAssetLocator locator = new NavigationAssetLocator();
            NavigationMapAssets assets = locator.CreateMapAssets(DefaultFolder, DefaultName);

            assets.MapAsset.SetRectangleSize(new Vector2(DefaultRectangleSizeMeters, DefaultRectangleSizeMeters));
            EditorUtility.SetDirty(assets.MapAsset);
            AssetDatabase.SaveAssets();

            GameObject mapObject = new GameObject("NavigationMap");
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            AssignMapData(map, assets.MapAsset);

            Selection.activeGameObject = mapObject;
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
    }
}
