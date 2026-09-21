using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class FormatMigratorTests
    {
        private FakeVersionedAsset asset;

        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<FakeVersionedAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void UpToDate_NoChange()
        {
            asset.SetFormatVersion(FakeVersionedAsset.FakeCurrentFormatVersion);
            FormatMigrator migrator = new FormatMigrator(new List<IFormatMigration>());

            FormatMigrationResult result = migrator.MigrateIfNeeded(asset);

            Assert.AreEqual(FormatMigrationResult.UpToDate, result);
            Assert.AreEqual(FakeVersionedAsset.FakeCurrentFormatVersion, asset.FormatVersion);
        }

        [Test]
        public void Older_AppliesStepsInOrder()
        {
            asset.SetFormatVersion(1);
            List<IFormatMigration> migrations = new List<IFormatMigration>();
            migrations.Add(new FakeFormatMigration(1));
            migrations.Add(new FakeFormatMigration(2));
            FormatMigrator migrator = new FormatMigrator(migrations);

            FormatMigrationResult result = migrator.MigrateIfNeeded(asset);

            Assert.AreEqual(FormatMigrationResult.Migrated, result);
            Assert.AreEqual(FakeVersionedAsset.FakeCurrentFormatVersion, asset.FormatVersion);
        }

        [Test]
        public void MissingStep_ReturnsMissingStep_StopsAtGap()
        {
            asset.SetFormatVersion(1);
            List<IFormatMigration> migrations = new List<IFormatMigration>();
            migrations.Add(new FakeFormatMigration(1));
            FormatMigrator migrator = new FormatMigrator(migrations);

            LogAssert.ignoreFailingMessages = true;
            FormatMigrationResult result = migrator.MigrateIfNeeded(asset);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(FormatMigrationResult.MissingStep, result);
            Assert.AreEqual(2, asset.FormatVersion);
        }

        [Test]
        public void Newer_NotModified_ReturnsNewerThanCode()
        {
            asset.SetFormatVersion(FakeVersionedAsset.FakeCurrentFormatVersion + 1);
            FormatMigrator migrator = new FormatMigrator(new List<IFormatMigration>());

            LogAssert.ignoreFailingMessages = true;
            FormatMigrationResult result = migrator.MigrateIfNeeded(asset);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(FormatMigrationResult.NewerThanCode, result);
            Assert.AreEqual(FakeVersionedAsset.FakeCurrentFormatVersion + 1, asset.FormatVersion);
        }

        private class FakeFormatMigration : IFormatMigration
        {
            private readonly int fromVersion;

            public Type AssetType { get { return typeof(FakeVersionedAsset); } }
            public int FromVersion { get { return fromVersion; } }

            public FakeFormatMigration(int fromVersion)
            {
                this.fromVersion = fromVersion;
            }

            public void Migrate(UnityEngine.Object asset)
            {
                FakeVersionedAsset fakeAsset = (FakeVersionedAsset)asset;
                fakeAsset.SetFormatVersion(fromVersion + 1);
            }
        }
    }
}
