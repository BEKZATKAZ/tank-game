using NUnit.Framework;
using Tanks.Shared.Weapons;
using Unity.Mathematics;

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

        [TestCase(1f / 15)]
        [TestCase(1f / 30)]
        [TestCase(1f / 60)]
        [TestCase(1f / 120)]
        public void RegularCooldown_CanUseRepeating(float deltaTime)
        {
            Cooldown cooldown = new Cooldown(0.2f);
            int iterationCount = (int)math.ceil(0.2f / deltaTime);

            for (int i = 0; i < 100; i++)
            {
                bool canUse = cooldown.CanUse();
                float elapsed = cooldown.Elapsed;

                if (i % iterationCount == 0)
                {
                    Assert.True(canUse,
                        $"Should be able to use by now, elapsed: {elapsed} / {0.2f}, i: {i}");
                }
                else
                {
                    Assert.False(canUse,
                        $"Should not be able to use now, elapsed {elapsed} / {0.2f}, i: {i}");
                }

                if (canUse) cooldown.Use();
                cooldown.Tick(deltaTime);
            }
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
