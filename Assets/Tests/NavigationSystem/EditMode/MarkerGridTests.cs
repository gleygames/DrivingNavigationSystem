using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MarkerGridTests
    {
        private MarkerGrid grid;
        private List<int> results;

        [SetUp]
        public void SetUp()
        {
            grid = new MarkerGrid();
            results = new List<int>();
        }

        [Test]
        public void QueryArea_ReturnsOnlyInside()
        {
            long insideCell = grid.ComputeCell(new Vector3(10f, 0f, 10f));
            long outsideCell = grid.ComputeCell(new Vector3(500f, 0f, 500f));
            grid.Add(0, insideCell);
            grid.Add(1, outsideCell);

            grid.QueryArea(new Vector2(-50f, -50f), new Vector2(50f, 50f), results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(0, results[0]);
        }

        [Test]
        public void Move_ChangesCell()
        {
            long oldCell = grid.ComputeCell(new Vector3(10f, 0f, 10f));
            long newCell = grid.ComputeCell(new Vector3(500f, 0f, 500f));
            grid.Add(0, oldCell);

            grid.Move(0, oldCell, newCell);

            grid.QueryArea(new Vector2(-50f, -50f), new Vector2(50f, 50f), results);
            Assert.AreEqual(0, results.Count);

            grid.QueryArea(new Vector2(450f, 450f), new Vector2(550f, 550f), results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(0, results[0]);
        }
    }
}
