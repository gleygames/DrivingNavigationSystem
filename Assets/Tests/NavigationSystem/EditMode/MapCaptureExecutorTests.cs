using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class MapCaptureExecutorTests
    {
        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";
        private const string MapPath = "Assets/Tests/NavigationSystem/Temp/Test_Map.asset";
        private const string ImagePath = "Assets/Tests/NavigationSystem/Temp/Test_MapImage.png";
        private const int ColorTolerance = 3;

        private MapData mapData;
        private MapCaptureExecutor executor;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.SetRectangleCenter(new Vector3(0f, 0f, 0f));
            mapData.SetRectangleSize(new Vector2(100f, 50f));
            AssetDatabase.CreateAsset(mapData, MapPath);

            executor = new MapCaptureExecutor();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void Capture_EmptyScene_ProducesFillColorImage_AndLocksMap()
        {
            CaptureSettings settings = new CaptureSettings();
            settings.LongerSidePixels = 64;
            settings.FillColor = new Color(0.2f, 0.4f, 0.6f, 1f);

            bool success = executor.Capture(mapData, settings, 1f);

            Assert.IsTrue(success);
            Assert.IsTrue(mapData.Locked);
            Assert.AreEqual(MapImageState.Captured, mapData.ImageState);
            Assert.IsNotNull(mapData.Image);
            Assert.AreEqual(ImagePath, AssetDatabase.GetAssetPath(mapData.Image));

            Texture2D loaded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(loaded.LoadImage(File.ReadAllBytes(ImagePath)));
                Assert.AreEqual(64, loaded.width);
                Assert.AreEqual(32, loaded.height);

                Color32 expected = settings.FillColor;
                Color32[] pixels = loaded.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    AssertColorClose(expected, pixels[i]);
                }

                AssertColorClose(expected, mapData.OutsideMapColor);
            }
            finally
            {
                Object.DestroyImmediate(loaded);
            }
        }

        private void AssertColorClose(Color32 expected, Color32 actual)
        {
            Assert.LessOrEqual(Mathf.Abs(expected.r - actual.r), ColorTolerance, "red");
            Assert.LessOrEqual(Mathf.Abs(expected.g - actual.g), ColorTolerance, "green");
            Assert.LessOrEqual(Mathf.Abs(expected.b - actual.b), ColorTolerance, "blue");
        }
    }
}
