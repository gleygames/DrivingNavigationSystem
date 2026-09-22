using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineRenderWidthTests
    {
        private const int TextureSize = 256;
        private const int CenterPixel = TextureSize / 2;
        private const int ExpectedWidthPixels = 8;
        private const int WidthTolerancePixels = 1;
        private const float BackgroundThreshold = 0.05f;
        private const float LineHalfLength = 60f;
        private const float ContainerScale = 0.5f;
        private const float PlaneDistance = 10f;

        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<float> distances = new List<float>();
        private readonly List<bool> dashed = new List<bool>();

        private GameObject cameraObject;
        private GameObject canvasObject;
        private RenderTexture renderTexture;
        private Texture2D readback;
        private Camera renderCamera;
        private RectTransform rootTransform;

        [SetUp]
        public void SetUp()
        {
            renderTexture = new RenderTexture(TextureSize, TextureSize, 24);
            readback = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);

            cameraObject = new GameObject("RenderWidthCamera");
            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.black;
            renderCamera.orthographic = true;
            renderCamera.targetTexture = renderTexture;

            canvasObject = new GameObject("RenderWidthCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = renderCamera;
            canvas.planeDistance = PlaneDistance;
            rootTransform = canvasObject.GetComponent<RectTransform>();

            points.Clear();
            distances.Clear();
            dashed.Clear();
            points.Add(new Vector2(-LineHalfLength, 0f));
            points.Add(new Vector2(LineHalfLength, 0f));
            distances.Add(0f);
            distances.Add(LineHalfLength * 2f);
            dashed.Add(false);
        }

        [UnityTest]
        public IEnumerator Render_NoNestedCanvas_LineIsDesignedWidth()
        {
            CreateGraphic(rootTransform);

            yield return RenderAndReadBack();

            int width = CountColumn(CenterPixel);
            Debug.Log("RenderWidth A (no nested canvas): " + width + " px");
            Assert.AreEqual(ExpectedWidthPixels, width, WidthTolerancePixels);
        }

        [UnityTest]
        public IEnumerator Render_NestedCanvasAboveScaledContainer_LineIsDesignedWidth()
        {
            RectTransform nested = CreateRect(rootTransform, "NestedCanvas");
            AddNestedCanvas(nested);
            RectTransform container = CreateRect(nested, "Container");
            container.localScale = new Vector3(ContainerScale, ContainerScale, 1f);
            CreateGraphic(container);

            yield return RenderAndReadBack();

            int width = CountColumn(CenterPixel);
            Debug.Log("RenderWidth B (nested canvas above scaled container): " + width + " px");
            Assert.AreEqual(ExpectedWidthPixels, width, WidthTolerancePixels);
        }

        [UnityTest]
        public IEnumerator Render_NestedCanvasBelowScaledContainer_LineIsDesignedWidth()
        {
            RectTransform container = CreateRect(rootTransform, "Container");
            container.localScale = new Vector3(ContainerScale, ContainerScale, 1f);
            RectTransform nested = CreateRect(container, "NestedCanvas");
            AddNestedCanvas(nested);
            CreateGraphic(nested);

            yield return RenderAndReadBack();

            int width = CountColumn(CenterPixel);
            Debug.Log("RenderWidth C (nested canvas below scaled container): " + width + " px");
            Assert.AreEqual(ExpectedWidthPixels, width, WidthTolerancePixels);
        }

        [UnityTest]
        public IEnumerator Render_NestedCanvasBelowScaledRotatedContainer_LineIsDesignedWidth()
        {
            RectTransform container = CreateRect(rootTransform, "Container");
            container.localScale = new Vector3(ContainerScale, ContainerScale, 1f);
            container.localRotation = Quaternion.Euler(0f, 0f, 90f);
            RectTransform nested = CreateRect(container, "NestedCanvas");
            AddNestedCanvas(nested);
            CreateGraphic(nested);

            yield return RenderAndReadBack();

            int width = CountRow(CenterPixel);
            Debug.Log("RenderWidth D (nested canvas below scaled + rotated container): " + width + " px");
            Assert.AreEqual(ExpectedWidthPixels, width, WidthTolerancePixels);
        }

        private RouteLineGraphic CreateGraphic(Transform parent)
        {
            RectTransform lineTransform = CreateRect(parent, "RouteLine");
            RouteLineGraphic graphic = lineTransform.gameObject.AddComponent<RouteLineGraphic>();
            graphic.SetLine(points, distances, dashed);
            return graphic;
        }

        private RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private IEnumerator RenderAndReadBack()
        {
            yield return WaitOneFrameAndUpdateCanvases();
            yield return WaitOneFrameAndUpdateCanvases();

            renderCamera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readback.ReadPixels(new Rect(0f, 0f, TextureSize, TextureSize), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;
        }

        private IEnumerator WaitOneFrameAndUpdateCanvases()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        private int CountColumn(int x)
        {
            int count = 0;
            for (int y = 0; y < TextureSize; y++)
            {
                if (IsLinePixel(readback.GetPixel(x, y)))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsLinePixel(Color pixel)
        {
            return pixel.r + pixel.g + pixel.b > BackgroundThreshold;
        }

        private void AddNestedCanvas(RectTransform target)
        {
            Canvas nestedCanvas = target.gameObject.AddComponent<Canvas>();
            nestedCanvas.overrideSorting = false;
        }

        private int CountRow(int y)
        {
            int count = 0;
            for (int x = 0; x < TextureSize; x++)
            {
                if (IsLinePixel(readback.GetPixel(x, y)))
                {
                    count++;
                }
            }

            return count;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(canvasObject);
            Object.Destroy(cameraObject);
            Object.Destroy(readback);
            renderTexture.Release();
            Object.Destroy(renderTexture);
        }
    }
}
