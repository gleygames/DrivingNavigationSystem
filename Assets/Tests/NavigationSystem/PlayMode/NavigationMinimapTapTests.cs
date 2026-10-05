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
            NavigationFullMap fullMap = CreateInactiveFullMap("FullMap");
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
            NavigationFullMap otherFullMap = CreateInactiveFullMap("OtherFullMap");
            NavigationFullMap assignedFullMap = CreateInactiveFullMap("AssignedFullMap");
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
            NavigationFullMap fullMap = CreateInactiveFullMap("FullMap");
            rig.Minimap.SetTapAction(MinimapTapAction.Nothing);

            rig.Minimap.OnPointerClick(new PointerEventData(null));

            Assert.IsFalse(fullMap.gameObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        private NavigationFullMap CreateInactiveFullMap(string name)
        {
            GameObject parentObject = new GameObject(name + "Parent", typeof(RectTransform));
            createdObjects.Add(parentObject);
            FullMapTestRig fullMapRig = new FullMapTestRig(parentObject.transform);
            fullMapRig.Root.name = name;
            createdObjects.Add(fullMapRig.Root);
            return fullMapRig.FullMap;
        }

        private void ExpectNoManagerErrorOnOpen()
        {
            LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"));
            LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"));
        }
    }
}
