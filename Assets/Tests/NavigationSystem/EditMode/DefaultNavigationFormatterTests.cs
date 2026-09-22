using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Gley.NavigationSystem.Tests
{
    public class DefaultNavigationFormatterTests
    {
        private DefaultNavigationFormatter formatter;
        private StringBuilder output;

        [SetUp]
        public void SetUp()
        {
            formatter = ScriptableObject.CreateInstance<DefaultNavigationFormatter>();
            output = new StringBuilder(32);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(formatter);
        }

        [Test]
        public void FormatDistance_ZeroMetersMetric_ReturnsZeroM()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Metric);
            formatter.FormatDistance(0f, output);
            Assert.AreEqual("0 m", output.ToString());
        }

        [Test]
        public void FormatDistance_847MetersMetric_Returns850M()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Metric);
            formatter.FormatDistance(847f, output);
            Assert.AreEqual("850 m", output.ToString());
        }

        [Test]
        public void FormatDistance_1234MetersMetric_Returns1Point2Km()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Metric);
            formatter.FormatDistance(1234f, output);
            Assert.AreEqual("1.2 km", output.ToString());
        }

        [Test]
        public void FormatDistance_12345MetersMetric_Returns12Km()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Metric);
            formatter.FormatDistance(12345f, output);
            Assert.AreEqual("12 km", output.ToString());
        }

        [Test]
        public void FormatDistance_30MetersImperial_Returns100Ft()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Imperial);
            formatter.FormatDistance(30f, output);
            Assert.AreEqual("100 ft", output.ToString());
        }

        [Test]
        public void FormatDistance_5000MetersImperial_Returns3Point1Mi()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Imperial);
            formatter.FormatDistance(5000f, output);
            Assert.AreEqual("3.1 mi", output.ToString());
        }

        [Test]
        public void FormatDuration_30Seconds_ReturnsLessThan1Min()
        {
            formatter.FormatDuration(30f, output);
            Assert.AreEqual("< 1 min", output.ToString());
        }

        [Test]
        public void FormatDuration_300Seconds_Returns5Min()
        {
            formatter.FormatDuration(300f, output);
            Assert.AreEqual("5 min", output.ToString());
        }

        [Test]
        public void FormatDuration_3900Seconds_Returns1H5Min()
        {
            formatter.FormatDuration(3900f, output);
            Assert.AreEqual("1 h 5 min", output.ToString());
        }

        [Test]
        public void ProjectSetting_ReadsImperialFromSettings()
        {
            NavigationSettings settings = ScriptableObject.CreateInstance<NavigationSettings>();
            settings.ResetToDefaults();
            settings.SetImperialUnits(true);
            formatter.SetSettings(settings);
            formatter.SetUnitSystem(NavigationUnitSystem.ProjectSetting);

            formatter.FormatDistance(30f, output);

            Assert.AreEqual("100 ft", output.ToString());
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void FormatDistance_NoGarbage()
        {
            formatter.SetUnitSystem(NavigationUnitSystem.Metric);
            output.Length = 0;
            formatter.FormatDistance(1234f, output);

            Assert.That(new TestDelegate(FormatOnce), Is.Not.AllocatingGCMemory());
        }

        private void FormatOnce()
        {
            output.Length = 0;
            formatter.FormatDistance(1234f, output);
        }
    }
}
