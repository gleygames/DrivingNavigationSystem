using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineMeshBuilderTests
    {
        private RouteLineMeshBuilder builder;
        private List<UIVertex> outVertices;
        private List<int> outIndices;

        [SetUp]
        public void SetUp()
        {
            builder = new RouteLineMeshBuilder();
            outVertices = new List<UIVertex>();
            outIndices = new List<int>();
        }

        [Test]
        public void Build_TwoPoints_Creates4Vertices6Indices()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 10f };
            List<bool> dashed = new List<bool> { false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(4, outVertices.Count);
            Assert.AreEqual(6, outIndices.Count);
        }

        [Test]
        public void Build_OnePoint_CreatesNothing()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f) };
            List<float> distances = new List<float> { 0f };
            List<bool> dashed = new List<bool>();

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(0, outVertices.Count);
            Assert.AreEqual(0, outIndices.Count);
        }

        [Test]
        public void Build_DuplicatePoints_AreSkipped()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 0f, 10f };
            List<bool> dashed = new List<bool> { false, false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(4, outVertices.Count);
            Assert.AreEqual(6, outIndices.Count);
            Assert.AreEqual(0f, outVertices[0].uv0.x, 0.001f);
            Assert.AreEqual(10f, outVertices[2].uv0.x, 0.001f);
        }

        [Test]
        public void Build_StraightLine_OffsetsArePerpendicularUnitLength()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 10f };
            List<bool> dashed = new List<bool> { false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            AssertVector2AreEqual(new Vector2(0f, 1f), outVertices[0].uv1);
            AssertVector2AreEqual(new Vector2(0f, -1f), outVertices[1].uv1);
        }

        [Test]
        public void Build_RightAngleCorner_UsesMiterWithScaleSqrt2()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10f, 10f) };
            List<float> distances = new List<float> { 0f, 10f, 20f };
            List<bool> dashed = new List<bool> { false, false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(6, outVertices.Count);
            Assert.AreEqual(1.41421356f, outVertices[2].uv1.magnitude, 0.001f);
        }

        [Test]
        public void Build_UTurn_UsesBevelTwoPairsAtCorner()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(0f, 0f) };
            List<float> distances = new List<float> { 0f, 10f, 20f };
            List<bool> dashed = new List<bool> { false, false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(8, outVertices.Count);
            Assert.AreEqual(18, outIndices.Count);
            AssertVector2AreEqual(new Vector2(0f, 1f), outVertices[2].uv1);
            AssertVector2AreEqual(new Vector2(0f, -1f), outVertices[4].uv1);
        }

        [Test]
        public void Build_Distances_AreCopiedToUv0X()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(5f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 5f, 10f };
            List<bool> dashed = new List<bool> { false, false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(0f, outVertices[0].uv0.x, 0.001f);
            Assert.AreEqual(5f, outVertices[2].uv0.x, 0.001f);
            Assert.AreEqual(10f, outVertices[4].uv0.x, 0.001f);
        }

        [Test]
        public void Build_DashedFlagChange_DuplicatesPairAtPoint()
        {
            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(5f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 5f, 10f };
            List<bool> dashed = new List<bool> { false, true };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(8, outVertices.Count);
            Assert.AreEqual(0f, outVertices[2].uv2.x, 0.001f);
            Assert.AreEqual(1f, outVertices[4].uv2.x, 0.001f);
            AssertVector2AreEqual(outVertices[2].position, outVertices[4].position);
        }

        [Test]
        public void Build_ReusedLists_AreClearedFirst()
        {
            outVertices.Add(new UIVertex());
            outVertices.Add(new UIVertex());
            outIndices.Add(999);

            List<Vector2> points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f) };
            List<float> distances = new List<float> { 0f, 10f };
            List<bool> dashed = new List<bool> { false };

            builder.Build(points, distances, dashed, outVertices, outIndices);

            Assert.AreEqual(4, outVertices.Count);
            Assert.AreEqual(6, outIndices.Count);
        }

        private void AssertVector2AreEqual(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
        }
    }
}
