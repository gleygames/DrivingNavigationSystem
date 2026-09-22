using NUnit.Framework;

namespace Gley.NavigationSystem.Tests
{
    public class CrosshairModeLogicTests
    {
        private CrosshairModeLogic logic;

        [SetUp]
        public void SetUp()
        {
            logic = new CrosshairModeLogic();
            logic.Mode = CrosshairMode.Auto;
        }

        [Test]
        public void Auto_CrosshairInput_Activates()
        {
            Assert.IsFalse(logic.IsCrosshairActive);

            logic.NotifyCrosshairInput();

            Assert.IsTrue(logic.IsCrosshairActive);
        }

        [Test]
        public void Auto_PointerInput_Deactivates()
        {
            logic.NotifyCrosshairInput();
            Assert.IsTrue(logic.IsCrosshairActive);

            logic.NotifyPointerInput();

            Assert.IsFalse(logic.IsCrosshairActive);
        }

        [Test]
        public void Always_IgnoresPointer()
        {
            logic.Mode = CrosshairMode.Always;

            logic.NotifyPointerInput();

            Assert.IsTrue(logic.IsCrosshairActive);
        }

        [Test]
        public void Never_IgnoresCrosshair()
        {
            logic.Mode = CrosshairMode.Never;

            logic.NotifyCrosshairInput();

            Assert.IsFalse(logic.IsCrosshairActive);
        }
    }
}
