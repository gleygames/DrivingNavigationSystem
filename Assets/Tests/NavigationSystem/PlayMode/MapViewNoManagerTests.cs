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
        public void MapView_EnabledWithoutManager_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("MapView on '.*': no NavigationManager found"));

            MapView view = testObject.AddComponent<MapView>();

            Assert.IsNull(view.Manager);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NavigationControls_EnabledWithoutManager_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("NavigationControls on '.*': no NavigationManager found"));

            testObject.AddComponent<NavigationControls>();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PreviewPanel_EnabledWithoutManager_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("PreviewPanel on '.*': no NavigationManager found"));

            testObject.AddComponent<PreviewPanel>();

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
