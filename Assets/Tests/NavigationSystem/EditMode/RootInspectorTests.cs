using System.Collections.Generic;
using Gley.NavigationSystem.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class RootInspectorTests
    {
        private GameObject rootObject;
        private UnityEditor.Editor createdEditor;
        private NavigationSettings settings;

        [Test]
        public void MinimapEditor_AllPathsExist()
        {
            NavigationMinimapEditor editor = CreateMinimapEditor();

            AssertAllPathsExist(editor.serializedObject, editor.VisiblePaths);
            AssertAllPathsExist(editor.serializedObject, editor.AdvancedPaths);
        }

        [Test]
        public void FullMapEditor_AllPathsExist()
        {
            NavigationFullMapEditor editor = CreateFullMapEditor();

            AssertAllPathsExist(editor.serializedObject, editor.VisiblePaths);
            AssertAllPathsExist(editor.serializedObject, editor.AdvancedPaths);
        }

        [Test]
        public void MinimapEditor_EverySettingIsShown()
        {
            NavigationMinimapEditor editor = CreateMinimapEditor();

            AssertEverySettingIsShown(editor.serializedObject, editor.VisiblePaths, editor.AdvancedPaths);
        }

        [Test]
        public void FullMapEditor_EverySettingIsShown()
        {
            NavigationFullMapEditor editor = CreateFullMapEditor();

            AssertEverySettingIsShown(editor.serializedObject, editor.VisiblePaths, editor.AdvancedPaths);
        }

        [Test]
        public void ManagerEditor_AllPathsExist()
        {
            NavigationManagerEditor editor = CreateManagerEditor();

            AssertAllPathsExist(editor.serializedObject, editor.VisiblePaths);
            AssertAllPathsExist(editor.serializedObject, editor.AdvancedPaths);
            AssertAllPathsExist(new SerializedObject(settings), editor.ProjectPaths);
        }

        private NavigationManagerEditor CreateManagerEditor()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            rootObject = new GameObject("InspectorTestManager");
            NavigationManager manager = rootObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            createdEditor = UnityEditor.Editor.CreateEditor(manager);
            return (NavigationManagerEditor)createdEditor;
        }

        [Test]
        public void ManagerEditor_EverySettingIsShown()
        {
            NavigationManagerEditor editor = CreateManagerEditor();

            AssertEverySettingIsShown(editor.serializedObject, editor.VisiblePaths, editor.AdvancedPaths);
        }

        [Test]
        public void ManagerEditor_EveryProjectSettingIsShown()
        {
            NavigationManagerEditor editor = CreateManagerEditor();
            Dictionary<string, int> listed = new Dictionary<string, int>();
            CountPaths(listed, editor.ProjectPaths);

            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty iterator = serializedSettings.FindProperty("runtime");
            SerializedProperty end = iterator.GetEndProperty();
            int propertyCount = 0;
            while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
            {
                if (iterator.propertyType == SerializedPropertyType.Generic)
                {
                    continue;
                }

                int count = 0;
                listed.TryGetValue(iterator.propertyPath, out count);
                Assert.AreEqual(1, count, iterator.propertyPath);
                propertyCount++;
            }

            Assert.AreEqual(editor.ProjectPaths.Length, propertyCount);
        }

        private NavigationMinimapEditor CreateMinimapEditor()
        {
            rootObject = new GameObject("InspectorTestMinimap", typeof(RectTransform));
            NavigationMinimap minimap = rootObject.AddComponent<NavigationMinimap>();
            createdEditor = UnityEditor.Editor.CreateEditor(minimap);
            return (NavigationMinimapEditor)createdEditor;
        }

        private NavigationFullMapEditor CreateFullMapEditor()
        {
            rootObject = new GameObject("InspectorTestFullMap", typeof(RectTransform));
            NavigationFullMap fullMap = rootObject.AddComponent<NavigationFullMap>();
            createdEditor = UnityEditor.Editor.CreateEditor(fullMap);
            return (NavigationFullMapEditor)createdEditor;
        }

        private void AssertAllPathsExist(SerializedObject serializedObject, string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                Assert.IsNotNull(serializedObject.FindProperty(paths[i]), paths[i]);
            }
        }

        private void AssertEverySettingIsShown(SerializedObject serializedObject, string[] visiblePaths, string[] advancedPaths)
        {
            Dictionary<string, int> listed = new Dictionary<string, int>();
            CountPaths(listed, visiblePaths);
            CountPaths(listed, advancedPaths);

            List<string> serialized = new List<string>();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                if (iterator.propertyType == SerializedPropertyType.Generic)
                {
                    continue;
                }

                serialized.Add(iterator.propertyPath);
            }

            Assert.Greater(serialized.Count, 0);

            for (int i = 0; i < serialized.Count; i++)
            {
                int count = 0;
                listed.TryGetValue(serialized[i], out count);
                Assert.AreEqual(1, count, serialized[i]);
            }

            foreach (KeyValuePair<string, int> pair in listed)
            {
                Assert.IsTrue(serialized.Contains(pair.Key), "Listed but not serialized: " + pair.Key);
            }
        }

        private void CountPaths(Dictionary<string, int> listed, string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                int count = 0;
                listed.TryGetValue(paths[i], out count);
                listed[paths[i]] = count + 1;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (createdEditor != null)
            {
                Object.DestroyImmediate(createdEditor);
            }

            if (rootObject != null)
            {
                Object.DestroyImmediate(rootObject);
            }

            if (settings != null)
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
