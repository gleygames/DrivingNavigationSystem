using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class NavigationAssetLocatorTests
    {
        private class SettingsBackup
        {
            public byte[] AssetBytes { get; set; }
            public byte[] MetaBytes { get; set; }
            public string OriginalPath { get; set; }
        }

        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";

        private readonly List<SettingsBackup> hiddenSettings = new List<SettingsBackup>();

        private NavigationAssetLocator locator;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            HideExistingSettingsAssets();

            locator = new NavigationAssetLocator(TempFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            RestoreHiddenSettingsAssets();
        }

        private void HideExistingSettingsAssets()
        {
            hiddenSettings.Clear();

            string[] guids = AssetDatabase.FindAssets("t:NavigationSettings");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                SettingsBackup backup = new SettingsBackup();
                backup.OriginalPath = path;
                backup.AssetBytes = File.ReadAllBytes(ToAbsolutePath(path));
                backup.MetaBytes = File.ReadAllBytes(ToAbsolutePath(path + ".meta"));
                hiddenSettings.Add(backup);

                AssetDatabase.DeleteAsset(path);
            }
        }

        private void RestoreHiddenSettingsAssets()
        {
            for (int i = 0; i < hiddenSettings.Count; i++)
            {
                SettingsBackup backup = hiddenSettings[i];
                File.WriteAllBytes(ToAbsolutePath(backup.OriginalPath), backup.AssetBytes);
                File.WriteAllBytes(ToAbsolutePath(backup.OriginalPath + ".meta"), backup.MetaBytes);
                AssetDatabase.ImportAsset(backup.OriginalPath, ImportAssetOptions.ForceSynchronousImport);
            }
            hiddenSettings.Clear();
        }

        private string ToAbsolutePath(string assetPath)
        {
            return Application.dataPath + assetPath.Substring("Assets".Length);
        }

        [Test]
        public void GetDefaultMapFolder_UsesSceneName()
        {
            string folder = locator.GetDefaultMapFolder("TestScene");

            Assert.AreEqual(TempFolder + "/TestScene", folder);
        }

        [Test]
        public void CreateMapAssets_CreatesThreeLinkedAssets()
        {
            string folder = TempFolder + "/TestScene";

            NavigationMapAssets assets = locator.CreateMapAssets(folder, "TestScene");

            Assert.IsNotNull(assets.MapAsset);
            Assert.IsNotNull(assets.AuthoringAsset);
            Assert.IsNotNull(assets.RuntimeAsset);
            Assert.AreEqual(folder + "/TestScene_Map.asset", AssetDatabase.GetAssetPath(assets.MapAsset));
            Assert.AreEqual(folder + "/TestScene_RoadsAuthoring.asset", AssetDatabase.GetAssetPath(assets.AuthoringAsset));
            Assert.AreEqual(folder + "/TestScene_RoadsRuntime.asset", AssetDatabase.GetAssetPath(assets.RuntimeAsset));
            Assert.AreEqual(assets.RuntimeAsset, assets.AuthoringAsset.RuntimeAsset);
            Assert.AreEqual(assets.MapAsset, assets.AuthoringAsset.MapAsset);
            Assert.AreEqual(assets.RuntimeAsset, assets.MapAsset.RoadNetwork);
        }

        [Test]
        public void FindOrCreateSettings_NoneExists_CreatesWithDefaults()
        {
            NavigationSettings settings = locator.FindOrCreateSettings();

            Assert.IsNotNull(settings);
            Assert.AreEqual(TempFolder + "/NavigationSettings.asset", AssetDatabase.GetAssetPath(settings));
            Assert.AreEqual(4, settings.RoadTypes.Count);
        }

        [Test]
        public void FindOrCreateSettings_Exists_ReturnsExisting()
        {
            NavigationSettings first = locator.FindOrCreateSettings();

            NavigationSettings second = locator.FindOrCreateSettings();

            Assert.AreEqual(first, second);
        }

        [Test]
        public void FindAuthoringFor_ReturnsMatchingAsset()
        {
            string folder = TempFolder + "/TestScene";
            NavigationMapAssets assets = locator.CreateMapAssets(folder, "TestScene");

            RoadNetworkAuthoring found = locator.FindAuthoringFor(assets.RuntimeAsset);

            Assert.AreEqual(assets.AuthoringAsset, found);
        }
    }
}
