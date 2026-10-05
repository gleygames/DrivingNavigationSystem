using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationManagerSettingsTests
    {
        private NavigationSettings settings;
        private NavigationManager manager;
        private DefaultNavigationFormatter settingsFormatter;
        private DefaultNavigationFormatter overrideFormatter;
        private GameObject managerObject;

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

        [Test]
        public void WithSettings_RuntimeSettingsComeFromAsset()
        {
            manager.Initialize();

            Assert.AreSame(settings.Runtime, manager.RuntimeSettings);
        }

        [Test]
        public void NoSettings_UsesBuiltInDefaults()
        {
            manager.SetSettings(null);
            manager.Initialize();

            Assert.IsNotNull(manager.RuntimeSettings);
            Assert.AreEqual(10f, manager.RuntimeSettings.ArrivalDistance, 0.001f);
        }

        [Test]
        public void FormatterInSettings_Used()
        {
            settingsFormatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            settings.Runtime.SetFormatter(settingsFormatter);

            manager.Initialize();

            Assert.AreSame(settingsFormatter, manager.Formatter);
        }

        [Test]
        public void SetFormatterBeforeInitialize_WinsOverSettings()
        {
            settingsFormatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            overrideFormatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            settings.Runtime.SetFormatter(settingsFormatter);
            manager.SetFormatter(overrideFormatter);

            manager.Initialize();

            Assert.AreSame(overrideFormatter, manager.Formatter);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(settings);
            if (settingsFormatter != null)
            {
                Object.DestroyImmediate(settingsFormatter);
            }
            if (overrideFormatter != null)
            {
                Object.DestroyImmediate(overrideFormatter);
            }
        }
    }
}
