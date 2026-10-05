using System.Text;
using Gley.NavigationSystem.TMP;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem.Tests
{
    public class TmpTextWriterTests
    {
        private TmpTextWriter writer;
        private GameObject gameObject;

        [SetUp]
        public void SetUp()
        {
            writer = ScriptableObject.CreateInstance<TmpTextWriter>();
            gameObject = new GameObject("TmpTextWriterTarget", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(writer);
        }

        [Test]
        public void CanWrite_TmpText_True()
        {
            TextMeshProUGUI tmp = gameObject.AddComponent<TextMeshProUGUI>();
            Assert.IsTrue(writer.CanWrite(tmp));
        }

        [Test]
        public void CanWrite_Image_False()
        {
            Image image = gameObject.AddComponent<Image>();
            Assert.IsFalse(writer.CanWrite(image));
        }

        [Test]
        public void Write_UpdatesText()
        {
            TextMeshProUGUI tmp = gameObject.AddComponent<TextMeshProUGUI>();
            writer.Write(tmp, new StringBuilder("1.2 km"));
            Assert.AreEqual("1.2 km", tmp.text);
        }

        [Test]
        public void FindText_FindsInactiveChild()
        {
            GameObject child = new GameObject("Child", typeof(RectTransform));
            child.transform.SetParent(gameObject.transform, false);
            TextMeshProUGUI tmp = child.AddComponent<TextMeshProUGUI>();
            child.SetActive(false);

            Assert.AreEqual(tmp, writer.FindText(gameObject));
        }
    }
}
