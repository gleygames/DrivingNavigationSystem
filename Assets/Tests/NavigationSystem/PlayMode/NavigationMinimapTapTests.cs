using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationMinimapTapTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        private MinimapTestRig rig;

        [SetUp]
        public void SetUp()
        {
            GameObject parentObject = new GameObject("MinimapParent", typeof(RectTransform));
            createdObjects.Add(parentObject);
            rig = new MinimapTestRig(parentObject.transform);
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
        public void Tap_FullMapUnassigned_FindsInactiveFullMap_Opens()
        {
            MapViewInteractive fullMap = CreateInactiveFullMap("FullMap");
            ExpectNoManagerErrorOnOpen();

            rig.Minimap.OnPointerClick(new PointerEventData(null));

            Assert.IsTrue(fullMap.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tap_NoFullMapInScene_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("NavigationMinimap on '.*': no full map"));

            rig.Minimap.OnPointerClick(new PointerEventData(null));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tap_FullMapAssigned_UsesAssigned()
        {
            MapViewInteractive otherFullMap = CreateInactiveFullMap("OtherFullMap");
            MapViewInteractive assignedFullMap = CreateInactiveFullMap("AssignedFullMap");
            rig.Minimap.SetFullMap(assignedFullMap);
            ExpectNoManagerErrorOnOpen();

            rig.Minimap.OnPointerClick(new PointerEventData(null));

            Assert.IsTrue(assignedFullMap.gameObject.activeSelf);
            Assert.IsFalse(otherFullMap.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tap_ActionNothing_DoesNothing()
        {
            MapViewInteractive fullMap = CreateInactiveFullMap("FullMap");
            rig.Minimap.SetTapAction(MinimapTapAction.Nothing);

            rig.Minimap.OnPointerClick(new PointerEventData(null));

            Assert.IsFalse(fullMap.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        private MapViewInteractive CreateInactiveFullMap(string name)
        {
            GameObject fullMapObject = new GameObject(name, typeof(RectTransform));
            fullMapObject.SetActive(false);
            createdObjects.Add(fullMapObject);
            return fullMapObject.AddComponent<MapViewInteractive>();
        }

        private void ExpectNoManagerErrorOnOpen()
        {
            LogAssert.Expect(LogType.Error, new Regex("MapView on '.*': no NavigationManager found"));
        }
    }
}
