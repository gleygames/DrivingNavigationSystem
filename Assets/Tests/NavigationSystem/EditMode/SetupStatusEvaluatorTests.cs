using System.Collections.Generic;
using NUnit.Framework;
using Gley.NavigationSystem.Editor;

namespace Gley.NavigationSystem.Tests
{
    public class SetupStatusEvaluatorTests
    {
        private SetupStatusEvaluator evaluator;

        [SetUp]
        public void SetUp()
        {
            evaluator = new SetupStatusEvaluator();
        }

        [Test]
        public void EvaluateMapArea_BothExist_ReturnsDone()
        {
            SetupStatus status = evaluator.EvaluateMapArea(true, true);
            Assert.AreEqual(SetupStatus.Done, status);
        }

        [Test]
        public void EvaluateMapArea_ObjectMissing_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateMapArea(false, true);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void EvaluateMapArea_AssetMissing_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateMapArea(true, false);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void EvaluateMapImage_None_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateMapImage(MapImageState.None, false);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void EvaluateMapImage_Outdated_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateMapImage(MapImageState.Outdated, false);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateMapImage_ImportWarnings_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateMapImage(MapImageState.Captured, true);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateMapImage_CapturedNoWarnings_ReturnsDone()
        {
            SetupStatus status = evaluator.EvaluateMapImage(MapImageState.Captured, false);
            Assert.AreEqual(SetupStatus.Done, status);
        }

        [Test]
        public void EvaluateRoads_NoRoads_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateRoads(0, false, 0);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void EvaluateRoads_BakeOutdated_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateRoads(3, true, 0);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateRoads_ValidationIssues_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateRoads(3, false, 2);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateRoads_AllGood_ReturnsDone()
        {
            SetupStatus status = evaluator.EvaluateRoads(3, false, 0);
            Assert.AreEqual(SetupStatus.Done, status);
        }

        [Test]
        public void EvaluateUi_ViewsMissing_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateUi(false, true, false, false);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void EvaluateUi_EventSystemMismatch_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateUi(true, true, true, false);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateUi_TmpMissing_ReturnsWarning()
        {
            SetupStatus status = evaluator.EvaluateUi(true, true, false, true);
            Assert.AreEqual(SetupStatus.Warning, status);
        }

        [Test]
        public void EvaluateUi_AllGood_ReturnsDone()
        {
            SetupStatus status = evaluator.EvaluateUi(true, true, false, false);
            Assert.AreEqual(SetupStatus.Done, status);
        }

        [Test]
        public void EvaluateCar_Assigned_ReturnsDone()
        {
            SetupStatus status = evaluator.EvaluateCar(true);
            Assert.AreEqual(SetupStatus.Done, status);
        }

        [Test]
        public void EvaluateCar_NotAssigned_ReturnsMissing()
        {
            SetupStatus status = evaluator.EvaluateCar(false);
            Assert.AreEqual(SetupStatus.Missing, status);
        }

        [Test]
        public void ChooseInputModule_NewEnabledPackagePresent_ReturnsInputSystemUI()
        {
            InputModuleChoice choice = evaluator.ChooseInputModule(true, true);
            Assert.AreEqual(InputModuleChoice.InputSystemUI, choice);
        }

        [Test]
        public void ChooseInputModule_NewEnabledPackageMissing_ReturnsStandalone()
        {
            InputModuleChoice choice = evaluator.ChooseInputModule(true, false);
            Assert.AreEqual(InputModuleChoice.Standalone, choice);
        }

        [Test]
        public void ChooseInputModule_OldInputPackagePresent_ReturnsStandalone()
        {
            InputModuleChoice choice = evaluator.ChooseInputModule(false, true);
            Assert.AreEqual(InputModuleChoice.Standalone, choice);
        }

        [Test]
        public void ChooseInputModule_OldInputPackageMissing_ReturnsStandalone()
        {
            InputModuleChoice choice = evaluator.ChooseInputModule(false, false);
            Assert.AreEqual(InputModuleChoice.Standalone, choice);
        }

        [Test]
        public void IsTrafficSystemPresent_Present_ReturnsTrue()
        {
            List<string> assemblyNames = new List<string>();
            assemblyNames.Add("Gley.NavigationSystem");
            assemblyNames.Add("Gley.TrafficSystem");

            bool result = evaluator.IsTrafficSystemPresent(assemblyNames);
            Assert.IsTrue(result);
        }

        [Test]
        public void IsTrafficSystemPresent_Absent_ReturnsFalse()
        {
            List<string> assemblyNames = new List<string>();
            assemblyNames.Add("Gley.NavigationSystem");
            assemblyNames.Add("Gley.Common");

            bool result = evaluator.IsTrafficSystemPresent(assemblyNames);
            Assert.IsFalse(result);
        }

        [Test]
        public void BlurFactor_Computes()
        {
            float factor = evaluator.BlurFactor(300f, 50f, 1f);
            Assert.AreEqual(6f, factor, 0.001f);
        }

        [Test]
        public void IsBlurry_Above2()
        {
            Assert.IsFalse(evaluator.IsBlurry(2f));
            Assert.IsTrue(evaluator.IsBlurry(2.01f));
        }
    }
}
