using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class RoadSelectionTests
    {
        private RoadSelection selection;

        [SetUp]
        public void SetUp()
        {
            selection = new RoadSelection();
        }

        private SelectionHit RoadHit(int roadId)
        {
            return new SelectionHit(SelectionHitKind.Road, roadId, -1, 0);
        }

        [Test]
        public void Click_Additive_AddsToSelection()
        {
            selection.Click(RoadHit(1), false);
            selection.Click(RoadHit(2), true);

            Assert.AreEqual(2, selection.RoadIds.Count);
            Assert.IsTrue(selection.Contains(1));
            Assert.IsTrue(selection.Contains(2));
        }

        [Test]
        public void Click_NotAdditive_Replaces()
        {
            selection.Click(RoadHit(1), false);
            selection.Click(new SelectionHit(SelectionHitKind.Intersection, 0, -1, 7), true);
            selection.Click(RoadHit(2), false);

            Assert.AreEqual(1, selection.RoadIds.Count);
            Assert.IsTrue(selection.Contains(2));
            Assert.IsFalse(selection.HasIntersection);
        }

        [Test]
        public void BoxSelect_SelectsRoadsWithPointInside()
        {
            List<int> roadIds = new List<int>();
            List<Vector2> guiPoints = new List<Vector2>();
            List<int> starts = new List<int>();

            roadIds.Add(10);
            starts.Add(guiPoints.Count);
            guiPoints.Add(new Vector2(5f, 5f));
            guiPoints.Add(new Vector2(500f, 500f));

            roadIds.Add(20);
            starts.Add(guiPoints.Count);
            guiPoints.Add(new Vector2(300f, 300f));
            guiPoints.Add(new Vector2(400f, 400f));

            roadIds.Add(30);
            starts.Add(guiPoints.Count);
            guiPoints.Add(new Vector2(900f, 900f));
            guiPoints.Add(new Vector2(50f, 60f));

            selection.BoxSelect(new Rect(0f, 0f, 100f, 100f), roadIds, guiPoints, starts, false);

            Assert.AreEqual(2, selection.RoadIds.Count);
            Assert.IsTrue(selection.Contains(10));
            Assert.IsFalse(selection.Contains(20));
            Assert.IsTrue(selection.Contains(30));
        }

        [Test]
        public void Clear_EmptiesEverything()
        {
            selection.Click(RoadHit(1), false);
            selection.Click(new SelectionHit(SelectionHitKind.KeyPoint, 2, 1, 0), true);
            selection.Click(new SelectionHit(SelectionHitKind.Intersection, 0, -1, 5), true);

            selection.Clear();

            Assert.AreEqual(0, selection.RoadIds.Count);
            Assert.IsFalse(selection.HasKeyPoint);
            Assert.IsFalse(selection.HasIntersection);
            Assert.IsTrue(selection.IsEmpty);
        }
    }
}
