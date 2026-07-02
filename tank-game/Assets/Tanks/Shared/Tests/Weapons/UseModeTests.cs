using NUnit.Framework;
using Tanks.Shared.Weapons;

namespace Tanks.Shared.Tests.Weapons
{
    public class UseModeTests
    {
        [Test]
        public void SemiAuto_CanUseOnce()
        {
            IUseMode useMode = new SemiAutomaticUseMode();
            useMode.StartUsing();
            Assert.IsTrue(useMode.IsUsing, "Should start using");

            useMode.Tick();
            Assert.IsFalse(useMode.IsUsing, "Should already stop using");
        }

        [Test]
        public void FullAuto_CanUseUntilReleasing()
        {
            IUseMode useMode = new FullAutomaticUseMode();
            useMode.StartUsing();

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(useMode.IsUsing);
                useMode.Tick();
            }

            useMode.StopUsing();
            Assert.IsFalse(useMode.IsUsing, "Should already stop using");
        }
    }
}