using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationMinimapCompassTests
    {
        private GameObject canvasObject;
        private MinimapTestRig rig;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rig = new MinimapTestRig(canvasObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
        }

        [Test]
        public void CompassClick_TogglesRotationMode()
        {
            Button button = rig.AddCompass();
            ExpectNoManagerError();
            rig.Activate();

            Assert.AreEqual(MinimapRotationMode.HeadingUp, rig.Minimap.RotationMode);

            button.onClick.Invoke();

            Assert.AreEqual(MinimapRotationMode.NorthUp, rig.Minimap.RotationMode);
        }

        [Test]
        public void NoCompass_EnablesWithoutOtherErrors()
        {
            ExpectNoManagerError();
            rig.Activate();

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CompassIcon_FollowsViewRotation()
        {
            Button button = rig.AddCompass();
            ExpectNoManagerError();
            rig.Activate();
            yield return null;

            rig.View.SetRotation(30f);
            yield return null;

            Assert.AreEqual(30f, button.transform.localEulerAngles.z, 0.01f);
        }

        private void ExpectNoManagerError()
        {
            LogAssert.Expect(LogType.Error, new Regex("no NavigationManager found"));
        }
    }
}
