using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineRendererTests
    {
        private GameObject canvasObject;
        private GameObject rendererObject;
        private RouteLineRenderer renderer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            rendererObject = new GameObject("RouteLineRenderer", typeof(RectTransform));
            rendererObject.transform.SetParent(canvasObject.transform, false);
            renderer = rendererObject.AddComponent<RouteLineRenderer>();

            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTest]
        public IEnumerator Enable_AddsOwnCanvas()
        {
            yield return null;

            Canvas ownCanvas = rendererObject.GetComponent<Canvas>();
            Assert.IsNotNull(ownCanvas);
        }

        [UnityTest]
        public IEnumerator SetLine_LongRoute_CreatesMultipleGraphics()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            renderer.SetLine(points, distances, dashed);
            yield return null;

            RouteLineGraphic[] graphics = rendererObject.GetComponentsInChildren<RouteLineGraphic>(true);
            Assert.AreEqual(3, graphics.Length);

            int activeCount = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].gameObject.activeSelf)
                {
                    activeCount++;
                }
            }

            Assert.AreEqual(3, activeCount);
        }

        [UnityTest]
        public IEnumerator SetLine_ShorterRoute_DisablesExtraGraphics()
        {
            List<Vector2> longPoints = new List<Vector2>();
            List<float> longDistances = new List<float>();
            List<bool> longDashed = new List<bool>();
            BuildStraightLine(10000, longPoints, longDistances, longDashed);
            renderer.SetLine(longPoints, longDistances, longDashed);
            yield return null;

            List<Vector2> shortPoints = new List<Vector2>();
            List<float> shortDistances = new List<float>();
            List<bool> shortDashed = new List<bool>();
            BuildStraightLine(10, shortPoints, shortDistances, shortDashed);
            renderer.SetLine(shortPoints, shortDistances, shortDashed);
            yield return null;

            RouteLineGraphic[] graphics = rendererObject.GetComponentsInChildren<RouteLineGraphic>(true);
            Assert.AreEqual(3, graphics.Length);
            Assert.IsTrue(graphics[0].gameObject.activeSelf);
            Assert.IsFalse(graphics[1].gameObject.activeSelf);
            Assert.IsFalse(graphics[2].gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SetTrimDistance_PastChunkEnd_DisablesChunk()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            renderer.SetStyle(Color.white, Color.black, Color.gray, 3f, 1f, 8f, 6f, 0);
            renderer.SetLine(points, distances, dashed);
            yield return null;

            renderer.SetTrimDistance(5000f);
            yield return null;

            RouteLineGraphic[] graphics = rendererObject.GetComponentsInChildren<RouteLineGraphic>(true);
            Assert.IsFalse(graphics[0].gameObject.activeSelf);
            Assert.IsTrue(graphics[1].gameObject.activeSelf);
            Assert.IsTrue(graphics[2].gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator SetTrimDistance_Back_ReenablesChunk()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            renderer.SetStyle(Color.white, Color.black, Color.gray, 3f, 1f, 8f, 6f, 0);
            renderer.SetLine(points, distances, dashed);
            yield return null;

            renderer.SetTrimDistance(5000f);
            yield return null;
            renderer.SetTrimDistance(0f);
            yield return null;

            RouteLineGraphic[] graphics = rendererObject.GetComponentsInChildren<RouteLineGraphic>(true);
            Assert.IsTrue(graphics[0].gameObject.activeSelf);
        }

        private void BuildStraightLine(int pointCount, List<Vector2> points, List<float> distances, List<bool> dashed)
        {
            for (int i = 0; i < pointCount; i++)
            {
                points.Add(new Vector2(i, 0f));
                distances.Add(i);
            }

            for (int i = 0; i < pointCount - 1; i++)
            {
                dashed.Add(false);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(canvasObject);
        }
    }
}
