using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class MapViewNoManagerTests
    {
        private GameObject testObject;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("NoManagerUi", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void NavigationMinimap_EnabledWithoutManager_LogsError()
        {
            MinimapTestRig rig = new MinimapTestRig(testObject.transform);
            LogAssert.Expect(LogType.Error, new Regex("NavigationMinimap on '.*': no NavigationManager found"));

            rig.Activate();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NavigationFullMap_EnabledWithoutManager_LogsError()
        {
            FullMapTestRig rig = new FullMapTestRig(testObject.transform);
            LogAssert.Expect(LogType.Error, new Regex("NavigationFullMap on '.*': no NavigationManager found"));

            rig.Activate();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NavigationEvents_EnabledWithoutManager_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("NavigationEvents on '.*': no NavigationManager found"));

            testObject.AddComponent<NavigationEvents>();

            LogAssert.NoUnexpectedReceived();
        }
    }
}
