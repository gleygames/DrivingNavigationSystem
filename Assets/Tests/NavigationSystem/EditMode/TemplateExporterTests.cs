using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class TemplateExporterTests
    {
        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";
        private const string TemplatePath = "Assets/Tests/NavigationSystem/Temp/Test_MapTemplate.png";

        private MapData mapData;
        private RoadNetworkData roadNetwork;
        private CapturePlanner planner;
        private TemplateExporter exporter;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.SetRectangleCenter(new Vector3(50f, 0f, 0f));
            mapData.SetRectangleSize(new Vector2(100f, 20f));
            mapData.SetRectangleRotationY(0f);

            TestNetworks networks = new TestNetworks();
            roadNetwork = networks.BuildNetwork(networks.Line(1, 100f));

            planner = new CapturePlanner();
            exporter = new TemplateExporter();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            Object.DestroyImmediate(mapData);
            Object.DestroyImmediate(roadNetwork);
        }

        [Test]
        public void MapToPixel_Corners()
        {
            ImageSizePlan plan = planner.PlanImageSize(new Vector2(100f, 20f), 64);

            Vector2Int origin = exporter.MapToPixel(Vector2.zero, plan);
            Vector2Int farCorner = exporter.MapToPixel(plan.AdjustedRectSize, plan);

            Assert.AreEqual(new Vector2Int(0, 0), origin);
            Assert.AreEqual(new Vector2Int(plan.WidthPx - 1, plan.HeightPx - 1), farCorner);
        }

        [Test]
        public void Export_DrawsRoadPixels()
        {
            ImageSizePlan plan = planner.PlanImageSize(mapData.RectangleSize, 64);

            exporter.Export(mapData, roadNetwork, plan, TemplatePath);

            Texture2D loaded = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(loaded.LoadImage(File.ReadAllBytes(TemplatePath)));

                MapFrame frame = new MapFrame(mapData.RectangleCenter, plan.AdjustedRectSize, mapData.RectangleRotationY);
                Vector2 mapMid = frame.TrueToMap(new Vector3(50f, 0f, 0f));
                Vector2Int pixelMid = exporter.MapToPixel(mapMid, plan);

                bool foundDark = false;
                for (int x = 0; x < loaded.width; x++)
                {
                    Color32 pixel = loaded.GetPixel(x, pixelMid.y);
                    if (pixel.r < 100 && pixel.g < 100 && pixel.b < 100)
                    {
                        foundDark = true;
                        break;
                    }
                }
                Assert.IsTrue(foundDark);

                Color32 backgroundColor = new Color32(128, 128, 128, 255);
                AssertColorClose(backgroundColor, loaded.GetPixel(0, 0));
                AssertColorClose(backgroundColor, loaded.GetPixel(loaded.width - 1, 0));
                AssertColorClose(backgroundColor, loaded.GetPixel(0, loaded.height - 1));
                AssertColorClose(backgroundColor, loaded.GetPixel(loaded.width - 1, loaded.height - 1));
            }
            finally
            {
                Object.DestroyImmediate(loaded);
            }
        }

        private void AssertColorClose(Color32 expected, Color32 actual)
        {
            Assert.LessOrEqual(Mathf.Abs(expected.r - actual.r), 3, "red");
            Assert.LessOrEqual(Mathf.Abs(expected.g - actual.g), 3, "green");
            Assert.LessOrEqual(Mathf.Abs(expected.b - actual.b), 3, "blue");
        }
    }
}
