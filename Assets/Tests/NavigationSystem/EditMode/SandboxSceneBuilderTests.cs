using Gley.NavigationSystem.Dev;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Gley.NavigationSystem.Tests
{
    public class SandboxSceneBuilderTests
    {
        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.CloseScene(testScene, true);
        }

        [Test]
        public void BuildCityObjects_DefaultSettings_CreatesExpectedRoadCount()
        {
            SandboxSceneBuilder builder = new SandboxSceneBuilder();
            builder.BuildCityObjects();

            Assert.AreEqual(SandboxSceneBuilder.ExpectedRoadObjectCount, builder.RoadObjects.Count);
        }
    }
}
