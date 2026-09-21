using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineGraphicTests
    {
        private const float Tolerance = 0.001f;

        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<float> distances = new List<float>();
        private readonly List<bool> dashed = new List<bool>();

        private GameObject canvasObject;
        private RectTransform container;
        private RouteLineGraphic graphic;
        private Canvas canvas;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            GameObject containerObject = new GameObject("Container", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(canvasObject.transform, false);

            GameObject lineObject = new GameObject("RouteLine", typeof(RectTransform));
            lineObject.transform.SetParent(container, false);
            graphic = lineObject.AddComponent<RouteLineGraphic>();

            points.Clear();
            distances.Clear();
            dashed.Clear();
            points.Add(new Vector2(0f, 0f));
            points.Add(new Vector2(10f, 0f));
            points.Add(new Vector2(10f, 10f));
            distances.Add(0f);
            distances.Add(10f);
            distances.Add(20f);
            dashed.Add(false);
            dashed.Add(true);

            yield return WaitOneFrameAndUpdateCanvases();
            yield return WaitOneFrameAndUpdateCanvases();
        }

        [UnityTest]
        public IEnumerator SetLine_BuildsMeshOnce()
        {
            int buildCountBefore = graphic.MeshBuildCount;

            graphic.SetLine(points, distances, dashed);
            yield return WaitOneFrameAndUpdateCanvases();
            yield return WaitOneFrameAndUpdateCanvases();

            Assert.AreEqual(buildCountBefore + 1, graphic.MeshBuildCount);
        }

        [UnityTest]
        public IEnumerator SetTrimDistance_DoesNotRebuildMesh()
        {
            graphic.SetLine(points, distances, dashed);
            yield return WaitOneFrameAndUpdateCanvases();
            int buildCountAfterLine = graphic.MeshBuildCount;

            graphic.SetTrimDistance(5f);
            yield return WaitOneFrameAndUpdateCanvases();

            Assert.AreEqual(buildCountAfterLine, graphic.MeshBuildCount);
            Assert.AreEqual(5f, graphic.materialForRendering.GetFloat("_TrimDistance"), Tolerance);
        }

        [UnityTest]
        public IEnumerator SetCanvasUnitsPerMeter_DoesNotRebuildMesh()
        {
            graphic.SetLine(points, distances, dashed);
            yield return WaitOneFrameAndUpdateCanvases();
            int buildCountAfterLine = graphic.MeshBuildCount;

            graphic.SetCanvasUnitsPerMeter(3f);
            yield return WaitOneFrameAndUpdateCanvases();

            Assert.AreEqual(buildCountAfterLine, graphic.MeshBuildCount);
            Assert.AreEqual(3f, graphic.materialForRendering.GetFloat("_CanvasUnitsPerMeter"), Tolerance);
        }

        [UnityTest]
        public IEnumerator OnEnable_AddsTexCoord1And2ShaderChannels()
        {
            yield return WaitOneFrameAndUpdateCanvases();

            AdditionalCanvasShaderChannels channels = canvas.additionalShaderChannels;
            Assert.AreEqual(AdditionalCanvasShaderChannels.TexCoord1, channels & AdditionalCanvasShaderChannels.TexCoord1);
            Assert.AreEqual(AdditionalCanvasShaderChannels.TexCoord2, channels & AdditionalCanvasShaderChannels.TexCoord2);
        }

        [UnityTest]
        public IEnumerator UpdateRouteLineVisuals_ContainerRotated_UpdatesOffsetMatrixWithoutRebuild()
        {
            graphic.SetLine(points, distances, dashed);
            yield return WaitOneFrameAndUpdateCanvases();
            int buildCountAfterLine = graphic.MeshBuildCount;

            container.localRotation = Quaternion.Euler(0f, 0f, 90f);
            container.localScale = new Vector3(4f, 4f, 1f);
            yield return WaitOneFrameAndUpdateCanvases();

            Vector4 matrix = graphic.CanvasOffsetMatrix;
            Assert.AreEqual(buildCountAfterLine, graphic.MeshBuildCount);
            Assert.AreEqual(0f, matrix.x, Tolerance);
            Assert.AreEqual(-1f, matrix.y, Tolerance);
            Assert.AreEqual(1f, matrix.z, Tolerance);
            Assert.AreEqual(0f, matrix.w, Tolerance);

            Vector4 materialMatrix = graphic.materialForRendering.GetVector("_CanvasOffsetMatrix");
            Assert.AreEqual(-1f, materialMatrix.y, Tolerance);
            Assert.AreEqual(1f, materialMatrix.z, Tolerance);
        }

        private IEnumerator WaitOneFrameAndUpdateCanvases()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(canvasObject);
        }
    }
}
