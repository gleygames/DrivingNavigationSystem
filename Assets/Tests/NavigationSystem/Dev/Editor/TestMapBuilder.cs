using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Dev
{
    public class TestMapBuilder
    {
        public const string DefaultFolder = "Assets/NavigationData/DevTestMap";
        public const string DefaultName = "DevTestMap";
        public const float DefaultRectangleSizeMeters = 200f;

        [MenuItem("Tools/Gley/Navigation Dev/Create Test Map")]
        private static void CreateTestMapMenuItem()
        {
            new TestMapBuilder().CreateTestMap();
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

        private void AssignMapData(NavigationMap map, MapData data)
        {
            SerializedObject serializedObject = new SerializedObject(map);
            serializedObject.FindProperty("mapData").objectReferenceValue = data;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
