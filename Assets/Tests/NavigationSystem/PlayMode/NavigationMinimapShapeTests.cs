using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationMinimapShapeTests
    {
        private GameObject canvasObject;
        private MinimapTestRig rig;
        private Texture2D texture;
        private Sprite sprite;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rig = new MinimapTestRig(canvasObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
            if (sprite != null)
            {
                Object.DestroyImmediate(sprite);
            }
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }

        [UnityTest]
        public IEnumerator Rectangle_AddsRectMask2D_RemovesMaskAndImage()
        {
            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Sprite);
            rig.Minimap.ApplyShape();
            yield return null;

            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Rectangle);
            rig.Minimap.ApplyShape();
            yield return null;

            Assert.IsNotNull(rig.Viewport.GetComponent<RectMask2D>());
            Assert.IsNull(rig.Viewport.GetComponent<Mask>());
            Assert.IsNull(rig.Viewport.GetComponent<Image>());
        }

        [UnityTest]
        public IEnumerator Sprite_AddsImageAndMask_RemovesRectMask2D()
        {
            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Rectangle);
            rig.Minimap.ApplyShape();
            yield return null;

            texture = new Texture2D(4, 4);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
            rig.Minimap.ShapeSettings.SetSprite(sprite);
            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Sprite);
            rig.Minimap.ApplyShape();
            yield return null;

            Image image = rig.Viewport.GetComponent<Image>();
            Assert.IsNotNull(image);
            Assert.AreEqual(sprite, image.sprite);
            Assert.IsTrue(image.raycastTarget);

            Mask mask = rig.Viewport.GetComponent<Mask>();
            Assert.IsNotNull(mask);
            Assert.IsFalse(mask.showMaskGraphic);

            Assert.IsNull(rig.Viewport.GetComponent<RectMask2D>());
        }

        [UnityTest]
        public IEnumerator ViewEdgeShape_FollowsShapeSettings()
        {
            LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"));
            rig.Activate();
            yield return null;

            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Rectangle);
            yield return null;
            Assert.AreEqual(EdgeShape.Rectangle, rig.View.EdgeShape);

            rig.Minimap.ShapeSettings.SetShapeKind(MinimapShapeKind.Sprite);
            rig.Minimap.ShapeSettings.SetSpriteOutline(EdgeShape.Circle);
            yield return null;
            Assert.AreEqual(EdgeShape.Circle, rig.View.EdgeShape);
        }
    }
}
