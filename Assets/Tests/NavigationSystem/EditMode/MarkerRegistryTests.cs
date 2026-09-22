using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Gley.NavigationSystem.Tests
{
    public class MarkerRegistryTests
    {
        private const int MinimapBit = 1 << 0;
        private const int FullMapBit = 1 << 1;
        private const int MovingMarkerCount = 500;

        private readonly List<GameObject> createdObjects = new List<GameObject>();

        private MarkerRegistry registry;
        private WorldConverter converter;

        [SetUp]
        public void SetUp()
        {
            registry = new MarkerRegistry();
            converter = new WorldConverter();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void AddRemove_ReusesIndex()
        {
            int first = registry.AddPoint(Vector3.zero, null, MinimapBit, false);
            registry.RemovePoint(first);
            int second = registry.AddPoint(Vector3.zero, null, MinimapBit, false);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void StaticMarker_ReadOnce_NotUpdated()
        {
            MapMarker marker = CreateMarker(new Vector3(5f, 0f, 5f), true);
            registry.AddObject(marker);
            registry.UpdateMarkerRegistryLogic(converter);
            int index = IndexOf(marker);
            Vector3 firstRead = registry.GetEntry(index).TruePosition;

            marker.transform.position = new Vector3(50f, 0f, 50f);
            registry.UpdateMarkerRegistryLogic(converter);
            Vector3 secondRead = registry.GetEntry(index).TruePosition;

            Assert.AreEqual(firstRead, secondRead);
            Assert.AreEqual(new Vector3(5f, 0f, 5f), secondRead);
        }

        [Test]
        public void MovingMarker_GridCellUpdated()
        {
            MapMarker marker = CreateMarker(new Vector3(5f, 0f, 5f), false);
            registry.AddObject(marker);
            registry.UpdateMarkerRegistryLogic(converter);
            int index = IndexOf(marker);
            long firstCell = registry.GetEntry(index).GridCell;

            marker.transform.position = new Vector3(500f, 0f, 500f);
            registry.UpdateMarkerRegistryLogic(converter);
            long secondCell = registry.GetEntry(index).GridCell;

            Assert.AreNotEqual(firstCell, secondCell);
            Assert.AreEqual(new Vector3(500f, 0f, 500f), registry.GetEntry(index).TruePosition);
        }

        [Test]
        public void QueryVisible_FiltersChannels()
        {
            int minimapOnly = registry.AddPoint(new Vector3(1f, 0f, 1f), null, MinimapBit, false);
            int fullMapOnly = registry.AddPoint(new Vector3(1f, 0f, 1f), null, FullMapBit, false);

            List<int> output = new List<int>();
            registry.QueryVisible(new Vector2(-10f, -10f), new Vector2(10f, 10f), MinimapBit, output);

            Assert.IsTrue(output.Contains(minimapOnly));
            Assert.IsFalse(output.Contains(fullMapOnly));
        }

        [Test]
        public void QueryVisible_IncludesArrowMarkersOutsideArea()
        {
            int farArrow = registry.AddPoint(new Vector3(5000f, 0f, 5000f), null, MinimapBit, true);

            List<int> output = new List<int>();
            registry.QueryVisible(new Vector2(-10f, -10f), new Vector2(10f, 10f), MinimapBit, output);

            Assert.IsTrue(output.Contains(farArrow));
        }

        [Test]
        public void UpdateMarkerRegistryLogic_500MovingMarkers_NoGarbage()
        {
            MapMarker[] markers = new MapMarker[MovingMarkerCount];
            for (int i = 0; i < MovingMarkerCount; i++)
            {
                markers[i] = CreateMarker(new Vector3(i * 100f + 50f, 0f, 0f), false);
                registry.AddObject(markers[i]);
            }

            registry.UpdateMarkerRegistryLogic(converter);
            MoveAll(markers, 1f);
            registry.UpdateMarkerRegistryLogic(converter);
            MoveAll(markers, 1f);
            registry.UpdateMarkerRegistryLogic(converter);
            MoveAll(markers, 1f);

            Assert.That(new TestDelegate(RunUpdate), Is.Not.AllocatingGCMemory());
        }

        private void RunUpdate()
        {
            registry.UpdateMarkerRegistryLogic(converter);
        }

        private void MoveAll(MapMarker[] markers, float step)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i].transform.position += new Vector3(step, 0f, 0f);
            }
        }

        private int IndexOf(MapMarker marker)
        {
            for (int i = 0; i < registry.EntryCount; i++)
            {
                if (registry.GetEntry(i).Marker == marker)
                {
                    return i;
                }
            }
            return -1;
        }

        private MapMarker CreateMarker(Vector3 position, bool isStatic)
        {
            GameObject gameObject = new GameObject("Marker");
            gameObject.transform.position = position;
            MapMarker marker = gameObject.AddComponent<MapMarker>();
            marker.SetIsStatic(isStatic);
            createdObjects.Add(gameObject);
            return marker;
        }
    }
}
