using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class SafeAreaFitterTests
    {
        private GameObject canvasObject;

        [TearDown]
        public void TearDown()
        {
            if (canvasObject != null)
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void WorldSpaceCanvas_NotChanged()
        {
            canvasObject = new GameObject("WorldCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject fitterObject = new GameObject("Fitter", typeof(RectTransform));
            fitterObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = fitterObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.2f);
            rect.anchorMax = new Vector2(0.7f, 0.8f);

            fitterObject.AddComponent<SafeAreaFitter>();

            Assert.AreEqual(new Vector2(0.1f, 0.2f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0.7f, 0.8f), rect.anchorMax);
        }
    }
}
