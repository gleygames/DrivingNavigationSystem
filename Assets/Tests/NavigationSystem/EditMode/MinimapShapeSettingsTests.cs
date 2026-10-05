using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class MinimapShapeSettingsTests
    {
        [Test]
        public void Outline_RectangleKind_IsRectangle()
        {
            MinimapShapeSettings settings = new MinimapShapeSettings();
            settings.SetShapeKind(MinimapShapeKind.Rectangle);
            settings.SetSpriteOutline(EdgeShape.Circle);

            Assert.AreEqual(EdgeShape.Rectangle, settings.Outline);
        }

        [Test]
        public void Outline_SpriteKind_UsesSpriteOutline()
        {
            MinimapShapeSettings settings = new MinimapShapeSettings();
            settings.SetShapeKind(MinimapShapeKind.Sprite);

            settings.SetSpriteOutline(EdgeShape.Circle);
            Assert.AreEqual(EdgeShape.Circle, settings.Outline);

            settings.SetSpriteOutline(EdgeShape.Rectangle);
            Assert.AreEqual(EdgeShape.Rectangle, settings.Outline);
        }
    }
}
