using Gley.NavigationSystem.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MapRectangleSyncTests
    {
        private MapRectangleSync sync;
        private MapData mapData;
        private GameObject gameObject;

        [SetUp]
        public void SetUp()
        {
            sync = new MapRectangleSync();
            mapData = ScriptableObject.CreateInstance<MapData>();
            gameObject = new GameObject("MapRectangleSyncTestObject");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mapData);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SnapObjectToAsset_SetsXZRotationScale_KeepsY()
        {
            mapData.SetRectangleCenter(new Vector3(20f, 0f, 30f));
            mapData.SetRectangleRotationY(45f);

            gameObject.transform.position = new Vector3(1f, 7f, 1f);
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(2f, 2f, 2f);

            sync.SnapObjectToAsset(gameObject.transform, mapData, 1f);

            Assert.AreEqual(20f, gameObject.transform.position.x, 0.001f);
            Assert.AreEqual(7f, gameObject.transform.position.y, 0.001f);
            Assert.AreEqual(30f, gameObject.transform.position.z, 0.001f);
            Assert.AreEqual(45f, gameObject.transform.eulerAngles.y, 0.001f);
            Assert.AreEqual(Vector3.one, gameObject.transform.localScale);
        }

        [Test]
        public void SnapObjectToAsset_UnitsPerMeter2_ScalesPosition()
        {
            mapData.SetRectangleCenter(new Vector3(50f, 0f, 50f));

            sync.SnapObjectToAsset(gameObject.transform, mapData, 2f);

            Assert.AreEqual(100f, gameObject.transform.position.x, 0.001f);
            Assert.AreEqual(100f, gameObject.transform.position.z, 0.001f);
        }

        [Test]
        public void ApplyTransformChange_Unlocked_WritesAsset_AndEditTimePosition()
        {
            mapData.SetRectangleCenter(Vector3.zero);
            mapData.SetRectangleRotationY(0f);
            mapData.SetLocked(false);

            gameObject.transform.position = new Vector3(10f, 3f, 20f);
            gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            MapRectangleSyncResult result = sync.ApplyTransformChange(gameObject.transform, mapData, 1f);

            Assert.AreEqual(MapRectangleSyncResult.Written, result);
            Assert.AreEqual(10f, mapData.RectangleCenter.x, 0.001f);
            Assert.AreEqual(3f, mapData.RectangleCenter.y, 0.001f);
            Assert.AreEqual(20f, mapData.RectangleCenter.z, 0.001f);
            Assert.AreEqual(90f, mapData.RectangleRotationY, 0.001f);
            Assert.AreEqual(new Vector3(10f, 3f, 20f), mapData.EditTimeWorldPosition);
        }

        [Test]
        public void ApplyTransformChange_Locked_RevertsXZAndRotation_KeepsY()
        {
            mapData.SetRectangleCenter(new Vector3(5f, 0f, 5f));
            mapData.SetRectangleRotationY(0f);
            mapData.SetLocked(true);

            gameObject.transform.position = new Vector3(50f, 8f, 60f);
            gameObject.transform.rotation = Quaternion.Euler(0f, 120f, 0f);

            MapRectangleSyncResult result = sync.ApplyTransformChange(gameObject.transform, mapData, 1f);

            Assert.AreEqual(MapRectangleSyncResult.Reverted, result);
            Assert.AreEqual(5f, gameObject.transform.position.x, 0.001f);
            Assert.AreEqual(8f, gameObject.transform.position.y, 0.001f);
            Assert.AreEqual(5f, gameObject.transform.position.z, 0.001f);
            Assert.AreEqual(0f, gameObject.transform.eulerAngles.y, 0.001f);
            Assert.AreEqual(8f, mapData.RectangleCenter.y, 0.001f);
        }

        [Test]
        public void ApplyTransformChange_ForcesScaleOne()
        {
            mapData.SetRectangleCenter(Vector3.zero);
            gameObject.transform.localScale = new Vector3(3f, 3f, 3f);

            sync.ApplyTransformChange(gameObject.transform, mapData, 1f);

            Assert.AreEqual(Vector3.one, gameObject.transform.localScale);
        }

        [Test]
        public void ResizeFromCorner_OppositeCornerFixed()
        {
            mapData.SetRectangleCenter(Vector3.zero);
            mapData.SetRectangleSize(new Vector2(100f, 100f));
            mapData.SetRectangleRotationY(0f);

            sync.ResizeFromCorner(mapData, 2, new Vector2(150f, 150f), false);

            Assert.AreEqual(150f, mapData.RectangleSize.x, 0.01f);
            Assert.AreEqual(150f, mapData.RectangleSize.y, 0.01f);

            MapFrame frame = mapData.CreateFrame();
            Vector3 bottomLeft = frame.MapToTrue(Vector2.zero, 0f);
            Assert.AreEqual(-50f, bottomLeft.x, 0.01f);
            Assert.AreEqual(-50f, bottomLeft.z, 0.01f);
        }

        [Test]
        public void SnapObjectToAsset_WritesEditTimePosition()
        {
            mapData.SetRectangleCenter(new Vector3(20f, 0f, 30f));
            mapData.SetEditTimeWorldPosition(Vector3.zero);
            gameObject.transform.position = new Vector3(1f, 7f, 1f);

            bool changed = sync.SnapObjectToAsset(gameObject.transform, mapData, 2f);

            Assert.IsTrue(changed);
            Assert.AreEqual(new Vector3(40f, 7f, 60f), mapData.EditTimeWorldPosition);
            Assert.AreEqual(gameObject.transform.position, mapData.EditTimeWorldPosition);
        }

        [Test]
        public void SnapObjectToAsset_AlreadyInSync_ReturnsFalse()
        {
            mapData.SetRectangleCenter(new Vector3(20f, 0f, 30f));
            gameObject.transform.position = new Vector3(5f, 2f, 5f);
            sync.SnapObjectToAsset(gameObject.transform, mapData, 1f);

            bool changed = sync.SnapObjectToAsset(gameObject.transform, mapData, 1f);

            Assert.IsFalse(changed);
        }

        [Test]
        public void ApplyTransformChange_Locked_WritesEditTimePosition()
        {
            mapData.SetRectangleCenter(new Vector3(5f, 0f, 5f));
            mapData.SetRectangleRotationY(0f);
            mapData.SetLocked(true);

            gameObject.transform.position = new Vector3(50f, 8f, 60f);

            sync.ApplyTransformChange(gameObject.transform, mapData, 1f);

            Assert.AreEqual(new Vector3(5f, 8f, 5f), mapData.EditTimeWorldPosition);
            Assert.AreEqual(gameObject.transform.position, mapData.EditTimeWorldPosition);
        }

        [Test]
        public void SnapSceneObjectsToAsset_AfterResize_ObjectAtNewCenter_NoShift()
        {
            mapData.SetRectangleCenter(Vector3.zero);
            mapData.SetRectangleSize(new Vector2(100f, 100f));
            mapData.SetRectangleRotationY(0f);
            NavigationMap map = gameObject.AddComponent<NavigationMap>();
            map.SetMapData(mapData);
            gameObject.transform.position = new Vector3(0f, 4f, 0f);
            sync.SnapObjectToAsset(gameObject.transform, mapData, 2f);

            sync.ResizeFromCorner(mapData, 2, new Vector2(150f, 150f), false);
            sync.SnapSceneObjectsToAsset(mapData, 2f, "Resize Map Rectangle");

            Assert.AreEqual(25f, mapData.RectangleCenter.x, 0.01f);
            Assert.AreEqual(25f, mapData.RectangleCenter.z, 0.01f);
            Assert.AreEqual(50f, gameObject.transform.position.x, 0.01f);
            Assert.AreEqual(4f, gameObject.transform.position.y, 0.01f);
            Assert.AreEqual(50f, gameObject.transform.position.z, 0.01f);

            Vector3 shift = gameObject.transform.position - mapData.EditTimeWorldPosition;
            Assert.AreEqual(0f, shift.magnitude, 0.001f);
        }

        [Test]
        public void SnapSceneObjectsToAsset_OtherMapData_NotMoved()
        {
            MapData otherData = ScriptableObject.CreateInstance<MapData>();
            otherData.SetRectangleCenter(new Vector3(500f, 0f, 500f));
            NavigationMap map = gameObject.AddComponent<NavigationMap>();
            map.SetMapData(mapData);
            gameObject.transform.position = new Vector3(3f, 0f, 3f);

            sync.SnapSceneObjectsToAsset(otherData, 1f, "Resize Map Rectangle");

            Assert.AreEqual(new Vector3(3f, 0f, 3f), gameObject.transform.position);
            Assert.AreEqual(Vector3.zero, otherData.EditTimeWorldPosition);
            Object.DestroyImmediate(otherData);
        }

        [Test]
        public void ResizeFromCorner_KeepRatio_PreservesAspect()
        {
            mapData.SetRectangleCenter(Vector3.zero);
            mapData.SetRectangleSize(new Vector2(100f, 50f));
            mapData.SetRectangleRotationY(0f);

            sync.ResizeFromCorner(mapData, 2, new Vector2(300f, 60f), true);

            float ratio = mapData.RectangleSize.x / mapData.RectangleSize.y;
            Assert.AreEqual(2f, ratio, 0.01f);
        }
    }
}
