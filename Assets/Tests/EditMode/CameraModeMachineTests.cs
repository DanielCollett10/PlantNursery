using NUnit.Framework;

namespace PlantNursery.Game.Tests
{
    public sealed class CameraModeMachineTests
    {
        [Test]
        public void StartsInFollow()
        {
            Assert.AreEqual(CameraMode.Follow, new CameraModeMachine(4f).Mode);
        }

        [Test]
        public void ZoomButtonZoomsOut_AnyInputReturns()
        {
            var m = new CameraModeMachine(4f);
            Assert.IsTrue(m.Update(0.016f, false, true));
            Assert.AreEqual(CameraMode.ZoomedOut, m.Mode);
            Assert.IsFalse(m.Update(0.016f, false, false));
            Assert.IsTrue(m.Update(0.016f, true, false));
            Assert.AreEqual(CameraMode.Follow, m.Mode);
        }

        [Test]
        public void IdleForConfiguredSecondsZoomsOut()
        {
            var m = new CameraModeMachine(4f);
            for (var i = 0; i < 39; i++) m.Update(0.1f, false, false);
            Assert.AreEqual(CameraMode.Follow, m.Mode);
            for (var i = 0; i < 3; i++) m.Update(0.1f, false, false);
            Assert.AreEqual(CameraMode.ZoomedOut, m.Mode);
        }

        [Test]
        public void InputResetsIdleTimer()
        {
            var m = new CameraModeMachine(4f);
            for (var i = 0; i < 30; i++) m.Update(0.1f, false, false);
            m.Update(0.1f, true, false);
            for (var i = 0; i < 30; i++) m.Update(0.1f, false, false);
            Assert.AreEqual(CameraMode.Follow, m.Mode);
        }

        [Test]
        public void ShorterConfigZoomsOutSooner()
        {
            var m = new CameraModeMachine(1f);
            for (var i = 0; i < 11; i++) m.Update(0.1f, false, false);
            Assert.AreEqual(CameraMode.ZoomedOut, m.Mode);
        }
    }
}
