using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class CustomImageAssignerTests
    {
        private MapData mapData;
        private Texture2D image;
        private CustomImageAssigner assigner;

        [SetUp]
        public void SetUp()
        {
            mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.SetRectangleCenter(new Vector3(10f, 0f, 20f));
            mapData.SetRectangleSize(new Vector2(1000f, 500f));

            image = new Texture2D(1000, 733, TextureFormat.RGB24, false);
            image.hideFlags = HideFlags.HideAndDontSave;

            assigner = new CustomImageAssigner();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mapData);
            Object.DestroyImmediate(image);
        }

        [Test]
        public void Assign_SetsCustomState_AdjustsHeightToImageRatio_KeepsCenter()
        {
            assigner.Assign(mapData, image);

            Assert.AreEqual(MapImageState.Custom, mapData.ImageState);
            Assert.IsFalse(mapData.Locked);
            Assert.AreEqual(image, mapData.Image);
            Assert.AreEqual(1000f, mapData.RectangleSize.x, 0.01f);
            Assert.AreEqual(733f, mapData.RectangleSize.y, 0.01f);
            Assert.AreEqual(new Vector3(10f, 0f, 20f), mapData.RectangleCenter);
        }

        [Test]
        public void GetGuidance_1000x733_RatioText()
        {
            mapData.SetRectangleSize(new Vector2(1000f, 733f));

            ImageGuidance guidance = assigner.GetGuidance(mapData);

            Assert.AreEqual("1000 × 733 m → 1.364 : 1", guidance.RatioText);
        }

        [Test]
        public void GetGuidance_RecommendedSizesAreMultiplesOf4()
        {
            mapData.SetRectangleSize(new Vector2(1000f, 733f));

            ImageGuidance guidance = assigner.GetGuidance(mapData);

            Assert.AreEqual(0, guidance.Recommended2048.WidthPx % 4);
            Assert.AreEqual(0, guidance.Recommended2048.HeightPx % 4);
            Assert.AreEqual(0, guidance.Recommended4096.WidthPx % 4);
            Assert.AreEqual(0, guidance.Recommended4096.HeightPx % 4);
        }
    }
}
