using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class MinimapShapeTests
    {
        private GameObject viewportObject;
        private MinimapShape shape;

        [SetUp]
        public void SetUp()
        {
            viewportObject = new GameObject("Minimap", typeof(RectTransform));
            shape = viewportObject.AddComponent<MinimapShape>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(viewportObject);
        }

        [UnityTest]
        public IEnumerator Rectangle_AddsRectMask2D_RemovesMask()
        {
            shape.SetShapeKind(MinimapShapeKind.Sprite);
            yield return null;
            Assert.IsNotNull(viewportObject.GetComponent<Mask>());

            shape.SetShapeKind(MinimapShapeKind.Rectangle);
            yield return null;

            Assert.IsNotNull(viewportObject.GetComponent<RectMask2D>());
            Assert.IsNull(viewportObject.GetComponent<Mask>());
        }

        [UnityTest]
        public IEnumerator Sprite_AddsMask_RemovesRectMask2D()
        {
            shape.SetShapeKind(MinimapShapeKind.Rectangle);
            yield return null;
            Assert.IsNotNull(viewportObject.GetComponent<RectMask2D>());

            shape.SetShapeKind(MinimapShapeKind.Sprite);
            yield return null;

            Assert.IsNotNull(viewportObject.GetComponent<Mask>());
            Assert.IsNull(viewportObject.GetComponent<RectMask2D>());
        }
    }
}
