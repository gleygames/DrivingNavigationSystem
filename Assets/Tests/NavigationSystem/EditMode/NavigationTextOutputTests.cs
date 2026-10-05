using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationTextOutputTests
    {
        private NavigationTextOutput output;
        private GameObject gameObject;
        private Image image;

        [SetUp]
        public void SetUp()
        {
            output = new NavigationTextOutput();
            gameObject = new GameObject("TextOutputTarget", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Write_LegacyText_NoWriter_Updates()
        {
            Text legacy = gameObject.AddComponent<Text>();
            output.Write(legacy, null, new StringBuilder("350 m"));
            Assert.AreEqual("350 m", legacy.text);
        }

        [Test]
        public void Write_UnknownComponent_NoWriter_DoesNothing()
        {
            image = gameObject.AddComponent<Image>();
            Assert.DoesNotThrow(WriteToImage);
        }

        [Test]
        public void Write_Null_DoesNothing()
        {
            Assert.DoesNotThrow(WriteToNull);
        }

        [Test]
        public void CanWrite_LegacyText_True()
        {
            Text legacy = gameObject.AddComponent<Text>();
            Assert.IsTrue(output.CanWrite(legacy, null));
        }

        [Test]
        public void CanWrite_Image_NoWriter_False()
        {
            Image image = gameObject.AddComponent<Image>();
            Assert.IsFalse(output.CanWrite(image, null));
        }

        [Test]
        public void FindText_LegacyChild_Found()
        {
            GameObject child = new GameObject("Child", typeof(RectTransform));
            child.transform.SetParent(gameObject.transform, false);
            Text legacy = child.AddComponent<Text>();

            Assert.AreEqual(legacy, output.FindText(gameObject, null));
        }

        private void WriteToImage()
        {
            output.Write(image, null, new StringBuilder("350 m"));
        }

        private void WriteToNull()
        {
            output.Write(null, null, new StringBuilder("350 m"));
        }
    }
}
