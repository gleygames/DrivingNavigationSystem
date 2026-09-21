using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class MapImageImportSettingsTests
    {
        private const string ParentFolder = "Assets/Tests/NavigationSystem";
        private const string TempFolder = "Assets/Tests/NavigationSystem/Temp";
        private const string ImagePath = "Assets/Tests/NavigationSystem/Temp/Test_MapImage.png";

        private MapImageImportSettings importSettings;

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
            AssetDatabase.CreateFolder(ParentFolder, "Temp");

            importSettings = new MapImageImportSettings();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        private void CreatePng(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(100, 120, 140, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            File.WriteAllBytes(ImagePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceUpdate);
        }

        private TextureImporter GetImporter()
        {
            return (TextureImporter)AssetImporter.GetAtPath(ImagePath);
        }

        [Test]
        public void ApplyIfNew_New_SetsNpotNoneMipmapsClamp()
        {
            CreatePng(64, 32);

            importSettings.ApplyIfNew(ImagePath, true, 64);

            TextureImporter importer = GetImporter();
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.IsTrue(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.AreEqual(TextureImporterType.Default, importer.textureType);
            Assert.IsTrue(importer.sRGBTexture);
            Assert.IsFalse(importer.isReadable);
            Assert.AreEqual(64, importer.maxTextureSize);
            Assert.AreEqual(TextureImporterCompression.Compressed, importer.textureCompression);
        }

        [Test]
        public void ApplyIfNew_New_MobileOverrides4096()
        {
            CreatePng(64, 32);

            importSettings.ApplyIfNew(ImagePath, true, 64);

            TextureImporter importer = GetImporter();
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            TextureImporterPlatformSettings iPhone = importer.GetPlatformTextureSettings("iPhone");
            Assert.IsTrue(android.overridden);
            Assert.AreEqual(4096, android.maxTextureSize);
            Assert.IsTrue(iPhone.overridden);
            Assert.AreEqual(4096, iPhone.maxTextureSize);
        }

        [Test]
        public void ApplyIfNew_NotNew_DoesNotChangeUserSettings()
        {
            CreatePng(64, 32);
            TextureImporter importer = GetImporter();
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();

            importSettings.ApplyIfNew(ImagePath, false, 64);

            importer = GetImporter();
            Assert.AreEqual(TextureImporterNPOTScale.ToNearest, importer.npotScale);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
            Assert.IsFalse(importer.GetPlatformTextureSettings("Android").overridden);
        }

        [Test]
        public void GetWarnings_BadSettings_ReturnsMessages()
        {
            CreatePng(30, 30);
            TextureImporter importer = GetImporter();
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.mipmapEnabled = false;
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 8192;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();

            List<string> warnings = new List<string>();
            importSettings.GetWarnings(ImagePath, warnings);

            Assert.AreEqual(4, warnings.Count);
        }

        [Test]
        public void GetWarnings_GoodSettings_ReturnsNoMessages()
        {
            CreatePng(64, 32);
            importSettings.ApplyIfNew(ImagePath, true, 64);

            List<string> warnings = new List<string>();
            importSettings.GetWarnings(ImagePath, warnings);

            Assert.AreEqual(0, warnings.Count);
        }
    }
}
