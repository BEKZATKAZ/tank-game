using NUnit.Framework;
using Tanks.Shared.Weapons;

namespace Tanks.Shared.Tests.Weapons
{
    public class CooldownTests
    {
        [Test]
        public void RegularCooldown_CanUse()
        {
            ICooldown cooldown = new Cooldown(1);
            Assert.IsTrue(cooldown.CanUse(), "Should be able to use initially");

            cooldown.Use();
            Assert.IsFalse(cooldown.CanUse(),
                "Should not be able to use immediately after consuming");
            
            cooldown.Tick(0.5f);
            Assert.IsFalse(cooldown.CanUse(),
                "Should not be able to use as time has not fully passed");

            cooldown.Tick(0.5f);
            Assert.IsTrue(cooldown.CanUse(),
                "Should be able to consume cooldown after time has passed");
        }

        [Test]
        public void NoCooldown_CanUseInfinitely()
        {
            ICooldown cooldown = new NoCooldown();
            Assert.IsTrue(cooldown.CanUse(), "Should be able to use initially");

            for (int i = 0; i < 100; i++)
            {
                cooldown.Use();
                Assert.IsTrue(cooldown.CanUse(),
                    "No cooldown is not an infinite cooldown");
            }
        }
    }
}