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
            useMode.Tick(true);
            Assert.IsTrue(useMode.IsUsing, "Should start using");

            useMode.Tick(true);
            Assert.IsFalse(useMode.IsUsing, "Should already stop using");
        }

        [Test]
        public void FullAuto_CanUseUntilReleasing()
        {
            IUseMode useMode = new FullAutomaticUseMode();
            useMode.Tick(true);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(useMode.IsUsing);
                useMode.Tick(true);
            }

            useMode.Tick(false);
            Assert.IsFalse(useMode.IsUsing, "Should already stop using");
        }
    }
}
