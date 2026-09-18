using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gley.NavigationSystem.Dev
{
    public class SandboxSceneBuilder
    {
        public const string ScenePath = "Assets/Tests/NavigationSystem/Dev/Scenes/Sandbox.unity";
        public const string RoadLayerName = "Road";
        public const int GridBlockCount = 6;
        public const float BlockSizeMeters = 100f;
        public const float RoadWidthMeters = 12f;
        public const float RoadHeightMeters = 0.2f;
        public const float GroundSizeMeters = 1000f;
        public const int BuildingRandomSeed = 1234;
        public const float BuildingMinHeightMeters = 5f;
        public const float BuildingMaxHeightMeters = 40f;
        public const float BridgeDeckHeightMeters = 6f;
        public const float BridgeRampLengthMeters = 40f;
        public const float CarWidthMeters = 2f;
        public const float CarHeightMeters = 1f;
        public const float CarLengthMeters = 4f;
        public const int GridLineCount = (GridBlockCount + 1) * 2;
        public const int DiagonalRoadCount = 1;
        public const int BridgePartCount = 3;
        public const int ExpectedRoadObjectCount = GridLineCount + DiagonalRoadCount + BridgePartCount;

        private readonly List<GameObject> roadObjects;

        private Transform carTransform;

        public IReadOnlyList<GameObject> RoadObjects { get; private set; }

        public SandboxSceneBuilder()
        {
            roadObjects = new List<GameObject>();
        }

        [MenuItem("Tools/Gley/Navigation Dev/Create Sandbox Scene")]
        private static void CreateSandboxSceneMenuItem()
        {
            new SandboxSceneBuilder().CreateSandboxScene();
        }

        public void CreateSandboxScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCityObjects();
            EnsureSceneFolderExists();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private void EnsureSceneFolderExists()
        {
            string folderPath = "Assets/Tests/NavigationSystem/Dev/Scenes";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets/Tests/NavigationSystem/Dev", "Scenes");
        }

        public GameObject BuildCityObjects()
        {
            roadObjects.Clear();

            GameObject root = new GameObject("Sandbox");
            BuildGround(root.transform);
            BuildRoadGrid(root.transform);
            BuildDiagonalRoad(root.transform);
            BuildBridge(root.transform);
            BuildBuildings(root.transform);
            BuildCar(root.transform);
            BuildFollowCamera(root.transform);
            BuildDirectionalLight(root.transform);

            RoadObjects = roadObjects;
            return root;
        }

        private void BuildGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(GroundSizeMeters, 1f, GroundSizeMeters);
        }

        private void BuildRoadGrid(Transform parent)
        {
            float gridSpanMeters = GridBlockCount * BlockSizeMeters;
            float gridStartMeters = gridSpanMeters * -0.5f;

            for (int i = 0; i <= GridBlockCount; i++)
            {
                float offsetMeters = gridStartMeters + i * BlockSizeMeters;

                Vector3 horizontalPosition = new Vector3(0f, RoadHeightMeters * 0.5f, offsetMeters);
                Vector3 horizontalSize = new Vector3(gridSpanMeters, RoadHeightMeters, RoadWidthMeters);
                GameObject horizontalSegment = CreateRoadSegment(horizontalPosition, horizontalSize, Quaternion.identity, "GridRoadHorizontal" + i);
                horizontalSegment.transform.SetParent(parent);

                Vector3 verticalPosition = new Vector3(offsetMeters, RoadHeightMeters * 0.5f, 0f);
                Vector3 verticalSize = new Vector3(RoadWidthMeters, RoadHeightMeters, gridSpanMeters);
                GameObject verticalSegment = CreateRoadSegment(verticalPosition, verticalSize, Quaternion.identity, "GridRoadVertical" + i);
                verticalSegment.transform.SetParent(parent);
            }
        }

        private GameObject CreateRoadSegment(Vector3 position, Vector3 size, Quaternion rotation, string objectName)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = objectName;
            segment.transform.position = position;
            segment.transform.rotation = rotation;
            segment.transform.localScale = size;
            segment.layer = GetRoadLayer();
            roadObjects.Add(segment);
            return segment;
        }

        private int GetRoadLayer()
        {
            int layer = LayerMask.NameToLayer(RoadLayerName);
            if (layer < 0)
            {
                return 0;
            }

            return layer;
        }

        private void BuildDiagonalRoad(Transform parent)
        {
            float gridSpanMeters = GridBlockCount * BlockSizeMeters;
            float diagonalLengthMeters = Mathf.Sqrt(gridSpanMeters * gridSpanMeters * 2f);

            Vector3 position = new Vector3(0f, RoadHeightMeters * 0.5f, 0f);
            Vector3 size = new Vector3(RoadWidthMeters, RoadHeightMeters, diagonalLengthMeters);
            GameObject diagonalSegment = CreateRoadSegment(position, size, Quaternion.Euler(0f, 45f, 0f), "DiagonalRoad");
            diagonalSegment.transform.SetParent(parent);
        }

        private void BuildBridge(Transform parent)
        {
            Vector3 deckPosition = new Vector3(0f, BridgeDeckHeightMeters, 0f);
            Vector3 deckSize = new Vector3(RoadWidthMeters, RoadHeightMeters, RoadWidthMeters * 3f);
            GameObject deck = CreateRoadSegment(deckPosition, deckSize, Quaternion.identity, "BridgeDeck");
            deck.transform.SetParent(parent);

            float rampOffsetMeters = RoadWidthMeters * 1.5f + BridgeRampLengthMeters * 0.5f;
            BuildBridgeRamp(parent, -rampOffsetMeters, "BridgeRampNear");
            BuildBridgeRamp(parent, rampOffsetMeters, "BridgeRampFar");
        }

        private void BuildBridgeRamp(Transform parent, float centerZMeters, string objectName)
        {
            float pitchDegrees = Mathf.Atan2(BridgeDeckHeightMeters, BridgeRampLengthMeters) * Mathf.Rad2Deg;
            if (centerZMeters < 0f)
            {
                pitchDegrees = -pitchDegrees;
            }

            Vector3 position = new Vector3(0f, BridgeDeckHeightMeters * 0.5f, centerZMeters);
            Vector3 size = new Vector3(RoadWidthMeters, RoadHeightMeters, BridgeRampLengthMeters);
            GameObject ramp = CreateRoadSegment(position, size, Quaternion.Euler(pitchDegrees, 0f, 0f), objectName);
            ramp.transform.SetParent(parent);
        }

        private void BuildBuildings(Transform parent)
        {
            System.Random random = new System.Random(BuildingRandomSeed);
            float gridSpanMeters = GridBlockCount * BlockSizeMeters;
            float gridStartMeters = gridSpanMeters * -0.5f;

            for (int row = 0; row < GridBlockCount; row++)
            {
                for (int column = 0; column < GridBlockCount; column++)
                {
                    float centerX = gridStartMeters + BlockSizeMeters * 0.5f + column * BlockSizeMeters;
                    float centerZ = gridStartMeters + BlockSizeMeters * 0.5f + row * BlockSizeMeters;
                    float heightMeters = BuildingMinHeightMeters + (float)random.NextDouble() * (BuildingMaxHeightMeters - BuildingMinHeightMeters);

                    GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    building.name = "Building" + row + "_" + column;
                    building.transform.SetParent(parent);
                    building.transform.position = new Vector3(centerX, heightMeters * 0.5f, centerZ);
                    building.transform.localScale = new Vector3(BlockSizeMeters * 0.5f, heightMeters, BlockSizeMeters * 0.5f);
                }
            }
        }

        private void BuildCar(Transform parent)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "Car";
            car.transform.SetParent(parent);
            car.transform.position = new Vector3(0f, 0.5f, RoadWidthMeters);
            car.transform.localScale = new Vector3(CarWidthMeters, CarHeightMeters, CarLengthMeters);
            car.AddComponent<DevCarController>();
            carTransform = car.transform;
        }

        private void BuildFollowCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            DevFollowCamera followCamera = cameraObject.AddComponent<DevFollowCamera>();
            followCamera.Target = carTransform;
        }

        private void BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
        }
    }
}
