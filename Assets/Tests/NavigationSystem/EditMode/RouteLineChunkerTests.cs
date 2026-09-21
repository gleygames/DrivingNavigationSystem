using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RouteLineChunkerTests
    {
        private RouteLineChunker chunker;
        private List<RouteLineChunk> outChunks;

        [SetUp]
        public void SetUp()
        {
            chunker = new RouteLineChunker();
            outChunks = new List<RouteLineChunk>();
        }

        [Test]
        public void Split_SmallRoute_OneChunk()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(50, points, distances, dashed);

            chunker.Split(points, distances, dashed, outChunks);

            Assert.AreEqual(1, outChunks.Count);
            Assert.AreEqual(0, outChunks[0].StartIndex);
            Assert.AreEqual(50, outChunks[0].Count);
        }

        [Test]
        public void Split_10000Points_ThreeChunks()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            chunker.Split(points, distances, dashed, outChunks);

            Assert.AreEqual(3, outChunks.Count);
        }

        [Test]
        public void Split_ChunksShareBoundaryPoint()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            chunker.Split(points, distances, dashed, outChunks);

            for (int i = 0; i < outChunks.Count - 1; i++)
            {
                int expectedBoundary = outChunks[i].StartIndex + outChunks[i].Count - 1;
                Assert.AreEqual(expectedBoundary, outChunks[i + 1].StartIndex);
            }
        }

        [Test]
        public void Split_ChunkDistancesAreCorrect()
        {
            List<Vector2> points = new List<Vector2>();
            List<float> distances = new List<float>();
            List<bool> dashed = new List<bool>();
            BuildStraightLine(10000, points, distances, dashed);

            chunker.Split(points, distances, dashed, outChunks);

            for (int i = 0; i < outChunks.Count; i++)
            {
                int endIndex = outChunks[i].StartIndex + outChunks[i].Count - 1;
                Assert.AreEqual(distances[outChunks[i].StartIndex], outChunks[i].StartDistance, 0.001f);
                Assert.AreEqual(distances[endIndex], outChunks[i].EndDistance, 0.001f);
            }
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
    }
}
