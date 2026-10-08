using NUnit.Framework;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationRuntimeSettingsTests
    {
        private NavigationSettings settings;
        private DefaultNavigationFormatter formatter;
        private GameObject prefab;

        [Test]
        public void Defaults_MatchDesign()
        {
            NavigationRuntimeSettings runtime = new NavigationRuntimeSettings();

            AssertTuningDefaults(runtime);
            Assert.IsNull(runtime.Formatter);
            Assert.IsNull(runtime.PlayerMarkerPrefab);
            Assert.IsNull(runtime.DestinationMarkerPrefab);
            Assert.IsNull(runtime.PreviewPinPrefab);
            Assert.IsNull(runtime.DefaultMarkerPrefab);
        }

        private void AssertTuningDefaults(NavigationRuntimeSettings runtime)
        {
            Assert.AreEqual(5f, runtime.AvoidMultiplier, 0.001f);
            Assert.AreEqual(0.7f, runtime.PreferMultiplier, 0.001f);
            Assert.AreEqual(200f, runtime.StartSnapDistance, 0.001f);
            Assert.AreEqual(50f, runtime.DestinationSnapDistance, 0.001f);
            Assert.AreEqual(10f, runtime.ArrivalDistance, 0.001f);
            Assert.AreEqual(30f, runtime.TurnedAroundDistance, 0.001f);
            Assert.AreEqual(20f, runtime.RerouteCooldown, 0.001f);
            Assert.AreEqual(1f, runtime.MinHeadingSpeed, 0.001f);
            Assert.AreEqual(0.1f, runtime.StoppedSpeed, 0.001f);
            Assert.AreEqual(50f, runtime.TeleportDistance, 0.001f);
            Assert.AreEqual(3f, runtime.LeaveMargin, 0.001f);
        }

        [Test]
        public void ResetTuningToDefaults_ResetsNumbers_KeepsReferences()
        {
            NavigationRuntimeSettings runtime = new NavigationRuntimeSettings();
            formatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            prefab = new GameObject("MarkerPrefab");
            runtime.SetFormatter(formatter);
            runtime.SetPlayerMarkerPrefab(prefab);
            runtime.SetDestinationMarkerPrefab(prefab);
            runtime.SetPreviewPinPrefab(prefab);
            runtime.SetDefaultMarkerPrefab(prefab);
            runtime.SetAvoidMultiplier(99f);
            runtime.SetPreferMultiplier(99f);
            runtime.SetStartSnapDistance(99f);
            runtime.SetDestinationSnapDistance(99f);
            runtime.SetArrivalDistance(99f);
            runtime.SetTurnedAroundDistance(99f);
            runtime.SetRerouteCooldown(99f);
            runtime.SetMinHeadingSpeed(99f);
            runtime.SetStoppedSpeed(99f);
            runtime.SetTeleportDistance(99f);
            runtime.SetLeaveMargin(99f);

            runtime.ResetTuningToDefaults();

            AssertTuningDefaults(runtime);
            Assert.AreSame(formatter, runtime.Formatter);
            Assert.AreSame(prefab, runtime.PlayerMarkerPrefab);
            Assert.AreSame(prefab, runtime.DestinationMarkerPrefab);
            Assert.AreSame(prefab, runtime.PreviewPinPrefab);
            Assert.AreSame(prefab, runtime.DefaultMarkerPrefab);
        }

        [Test]
        public void NavigationSettings_ResetToDefaults_ResetsRuntimeTuning()
        {
            settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.Runtime.SetLeaveMargin(9f);

            settings.ResetToDefaults();

            Assert.AreEqual(3f, settings.Runtime.LeaveMargin, 0.001f);
        }

        [TearDown]
        public void TearDown()
        {
            if (settings != null)
            {
                Object.DestroyImmediate(settings);
            }
            if (formatter != null)
            {
                Object.DestroyImmediate(formatter);
            }
            if (prefab != null)
            {
                Object.DestroyImmediate(prefab);
            }
        }
    }
}
