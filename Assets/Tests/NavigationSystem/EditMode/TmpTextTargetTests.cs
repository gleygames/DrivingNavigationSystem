using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class TmpTextTargetTests
    {
        private GameObject gameObject;
        private TextMeshProUGUI tmpText;
        private Gley.NavigationSystem.TMP.TmpTextTarget target;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TmpTextTarget");
            tmpText = gameObject.AddComponent<TextMeshProUGUI>();
            target = gameObject.AddComponent<Gley.NavigationSystem.TMP.TmpTextTarget>();
            AssignText(target, tmpText);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void TmpTextTarget_SetText_UpdatesText()
        {
            StringBuilder value = new StringBuilder("1.2 km");
            target.SetText(value);
            Assert.AreEqual("1.2 km", tmpText.text);
        }

        private void AssignText(Gley.NavigationSystem.TMP.TmpTextTarget textTarget, TMP_Text value)
        {
            SerializedObject serializedObject = new SerializedObject(textTarget);
            serializedObject.FindProperty("text").objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
