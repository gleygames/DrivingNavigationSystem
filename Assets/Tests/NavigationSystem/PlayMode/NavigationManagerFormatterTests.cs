using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationManagerFormatterTests
    {
        private NavigationSettings settings;
        private GameObject managerObject;
        private NavigationManager manager;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();

            managerObject = new GameObject("NavigationManager");
            manager = managerObject.AddComponent<NavigationManager>();
            manager.SetSettings(settings);
            manager.SetStartManually(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void NoFormatterAssigned_DefaultCreated()
        {
            manager.Initialize();

            Assert.IsNotNull(manager.Formatter);
            Assert.IsInstanceOf<DefaultNavigationFormatter>(manager.Formatter);
        }

        [Test]
        public void SetFormatterNull_FallsBackToDefault()
        {
            manager.Initialize();
            DefaultNavigationFormatter custom = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            manager.SetFormatter(custom);

            Assert.AreSame(custom, manager.Formatter);

            manager.SetFormatter(null);

            Assert.IsNotNull(manager.Formatter);
            Assert.IsInstanceOf<DefaultNavigationFormatter>(manager.Formatter);
            Assert.AreNotSame(custom, manager.Formatter);

            Object.DestroyImmediate(custom);
        }
    }
}
