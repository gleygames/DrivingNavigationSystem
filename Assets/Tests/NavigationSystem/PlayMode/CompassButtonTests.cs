using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class CompassButtonTests
    {
        private GameObject viewObject;
        private GameObject buttonObject;
        private MapViewFollowCar followCar;

        [SetUp]
        public void SetUp()
        {
            viewObject = new GameObject("Minimap", typeof(RectTransform));
            viewObject.AddComponent<MapView>();
            followCar = viewObject.AddComponent<MapViewFollowCar>();

            buttonObject = new GameObject("Compass", typeof(RectTransform));
            buttonObject.transform.SetParent(viewObject.transform, false);
            buttonObject.AddComponent<Image>();
            buttonObject.AddComponent<CompassButton>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(viewObject);
        }

        [Test]
        public void Click_TogglesRotationMode()
        {
            Assert.AreEqual(MinimapRotationMode.HeadingUp, followCar.RotationMode);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.Invoke();

            Assert.AreEqual(MinimapRotationMode.NorthUp, followCar.RotationMode);
        }
    }
}
