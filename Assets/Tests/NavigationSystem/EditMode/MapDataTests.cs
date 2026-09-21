using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MapDataTests
    {
        private MapData mapData;
        private RoadNetworkData roadNetworkData;
        private NavigationSettings navigationSettings;

        [SetUp]
        public void SetUp()
        {
            mapData = ScriptableObject.CreateInstance<MapData>();
            roadNetworkData = ScriptableObject.CreateInstance<RoadNetworkData>();
            navigationSettings = ScriptableObject.CreateInstance<NavigationSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mapData);
            Object.DestroyImmediate(roadNetworkData);
            Object.DestroyImmediate(navigationSettings);
        }

        [Test]
        public void CreateFrame_UsesRectangleValues()
        {
            mapData.SetRectangleCenter(new Vector3(50f, 0f, 50f));
            mapData.SetRectangleSize(new Vector2(100f, 100f));
            mapData.SetRectangleRotationY(0f);

            MapFrame frame = mapData.CreateFrame();
            Vector2 corner = frame.TrueToMap(new Vector3(0f, 0f, 0f));

            Assert.AreEqual(0f, corner.x, 0.01f);
            Assert.AreEqual(0f, corner.y, 0.01f);
        }

        [Test]
        public void NewAsset_FormatVersionIsCurrent()
        {
            Assert.AreEqual(MapData.CurrentFormatVersion, mapData.FormatVersion);
            Assert.AreEqual(RoadNetworkData.CurrentFormatVersion, roadNetworkData.FormatVersion);
            Assert.AreEqual(NavigationSettings.CurrentFormatVersion, navigationSettings.FormatVersion);
        }

        [Test]
        public void NewAsset_ImageStateIsNone_NotLocked()
        {
            Assert.AreEqual(MapImageState.None, mapData.ImageState);
            Assert.IsFalse(mapData.Locked);
        }
    }
}
