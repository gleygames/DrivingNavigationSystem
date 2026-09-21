using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class RoutePreferencesTests
    {
        [Test]
        public void GetMultiplier_Default_IsOne()
        {
            RoutePreferences preferences = new RoutePreferences();

            float multiplier = preferences.GetMultiplier(1);

            Assert.AreEqual(1f, multiplier, 0.001f);
        }

        [Test]
        public void GetMultiplier_Avoid_IsFive()
        {
            RoutePreferences preferences = new RoutePreferences();
            preferences.SetPreference(1, RoadTypePreference.Avoid);

            float multiplier = preferences.GetMultiplier(1);

            Assert.AreEqual(5f, multiplier, 0.001f);
        }

        [Test]
        public void GetMultiplier_Prefer_IsPointSeven()
        {
            RoutePreferences preferences = new RoutePreferences();
            preferences.SetPreference(1, RoadTypePreference.Prefer);

            float multiplier = preferences.GetMultiplier(1);

            Assert.AreEqual(0.7f, multiplier, 0.001f);
        }

        [Test]
        public void CopyFrom_CopiesAllValues()
        {
            RoutePreferences source = new RoutePreferences();
            source.Mode = RouteMode.Fastest;
            source.UTurn = UTurnRule.Anywhere;
            source.AvoidMultiplier = 4f;
            source.PreferMultiplier = 0.5f;
            source.SetPreference(1, RoadTypePreference.Avoid);
            source.SetPreference(2, RoadTypePreference.Prefer);

            RoutePreferences target = new RoutePreferences();
            target.CopyFrom(source);

            Assert.AreEqual(RouteMode.Fastest, target.Mode);
            Assert.AreEqual(UTurnRule.Anywhere, target.UTurn);
            Assert.AreEqual(4f, target.AvoidMultiplier, 0.001f);
            Assert.AreEqual(0.5f, target.PreferMultiplier, 0.001f);
            Assert.AreEqual(RoadTypePreference.Avoid, target.GetPreference(1));
            Assert.AreEqual(RoadTypePreference.Prefer, target.GetPreference(2));
        }
    }
}
