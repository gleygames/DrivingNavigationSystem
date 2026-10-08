using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class MarkerSelectionScaleTests
    {
        private GameObject markerObject;
        private MarkerSelectionScale selectionScale;

        [SetUp]
        public void SetUp()
        {
            markerObject = new GameObject("Marker");
            selectionScale = markerObject.AddComponent<MarkerSelectionScale>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(markerObject);
        }

        [Test]
        public void SelectedScale_Default_Is1Point25()
        {
            Assert.AreEqual(1.25f, selectionScale.SelectedScale, 0.0001f);
        }

        [Test]
        public void SetSelected_True_ScalesUp()
        {
            selectionScale.SetSelected(true);

            Assert.AreEqual(new Vector3(1.25f, 1.25f, 1.25f), markerObject.transform.localScale);
        }

        [Test]
        public void SetSelected_False_ResetsScale()
        {
            selectionScale.SetSelected(true);
            selectionScale.SetSelected(false);

            Assert.AreEqual(Vector3.one, markerObject.transform.localScale);
        }
    }
}
