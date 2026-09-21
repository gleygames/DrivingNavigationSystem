using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;
using Gley.NavigationSystem.Tests;

namespace Gley.NavigationSystem.Dev
{
    public class LargeNetworkGenerator
    {
        public const string DefaultFolder = "Assets/NavigationData/Dev5k";
        public const string DefaultName = "Dev5k";
        public const int DefaultRoadCount = 5000;
        public const int DefaultSeed = 1;
        public const float RectangleMargin = 100f;

        [MenuItem("Tools/Gley/Navigation Dev/Generate 5k Road Map")]
        private static void GenerateMenuItem()
        {
            new LargeNetworkGenerator().Generate();
        }

        public RoadNetworkAuthoring Generate()
        {
            NavigationAssetLocator locator = new NavigationAssetLocator();
            NavigationMapAssets assets = locator.CreateMapAssets(DefaultFolder, DefaultName);

            TestCityGenerator generator = new TestCityGenerator();
            RoadNetworkBuildInput input = generator.Generate(DefaultRoadCount, DefaultSeed);

            AuthoringFromBuildInput converter = new AuthoringFromBuildInput();
            converter.Fill(assets.AuthoringAsset, input);
            assets.AuthoringAsset.MarkChanged();

            NavigationSettings settings = locator.FindOrCreateSettings();
            AssignSettings(assets.AuthoringAsset, settings);
            SizeRectangle(assets.MapAsset, input);

            EditorUtility.SetDirty(assets.AuthoringAsset);
            EditorUtility.SetDirty(assets.MapAsset);
            AssetDatabase.SaveAssets();

            new RoadBaker().Bake(assets.AuthoringAsset);

            CreateMapObject(assets.MapAsset, settings.UnitsPerMeter);

            return assets.AuthoringAsset;
        }

        private void AssignSettings(RoadNetworkAuthoring authoring, NavigationSettings settings)
        {
            SerializedObject serializedObject = new SerializedObject(authoring);
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SizeRectangle(MapData mapAsset, RoadNetworkBuildInput input)
        {
            Vector3 min;
            Vector3 max;
            ComputeBounds(input, out min, out max);

            Vector3 center = new Vector3((min.x + max.x) * 0.5f, 0f, (min.z + max.z) * 0.5f);
            Vector2 size = new Vector2(max.x - min.x + RectangleMargin, max.z - min.z + RectangleMargin);

            mapAsset.SetRectangleCenter(center);
            mapAsset.SetRectangleSize(size);
        }

        private void ComputeBounds(RoadNetworkBuildInput input, out Vector3 min, out Vector3 max)
        {
            min = Vector3.zero;
            max = Vector3.zero;
            bool hasPoint = false;

            for (int i = 0; i < input.Roads.Count; i++)
            {
                BuildRoad road = input.Roads[i];
                for (int p = 0; p < road.Points.Count; p++)
                {
                    Vector3 point = road.Points[p];
                    if (!hasPoint)
                    {
                        min = point;
                        max = point;
                        hasPoint = true;
                        continue;
                    }

                    if (point.x < min.x)
                    {
                        min.x = point.x;
                    }
                    if (point.z < min.z)
                    {
                        min.z = point.z;
                    }
                    if (point.x > max.x)
                    {
                        max.x = point.x;
                    }
                    if (point.z > max.z)
                    {
                        max.z = point.z;
                    }
                }
            }
        }

        private void CreateMapObject(MapData mapAsset, float unitsPerMeter)
        {
            GameObject mapObject = new GameObject(DefaultName);
            NavigationMap map = mapObject.AddComponent<NavigationMap>();
            AssignMapData(map, mapAsset);

            MapRectangleSync sync = new MapRectangleSync();
            sync.SnapObjectToAsset(mapObject.transform, mapAsset, unitsPerMeter);

            Selection.activeGameObject = mapObject;
        }

        private void AssignMapData(NavigationMap map, MapData data)
        {
            SerializedObject serializedObject = new SerializedObject(map);
            serializedObject.FindProperty("mapData").objectReferenceValue = data;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
