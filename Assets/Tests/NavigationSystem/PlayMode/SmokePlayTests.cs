using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Gley.NavigationSystem.Tests
{
    public class SmokePlayTests
    {
        [UnityTest]
        public IEnumerator Smoke_AfterOneFrame_Passes()
        {
            yield return null;
            Assert.IsTrue(true);
        }
    }
}