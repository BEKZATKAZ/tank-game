using NUnit.Framework;
using System;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Tests.Weapons
{
    class MockAmmoConfig : IAmmoConfig
    {
        public AmmoType Type { get; set; }

        public int DefaultAmount { get; set; }
        public int MaxAmount { get; set; }

        public float RegenerationDelay { get; set; }
        public float RegenerationDuration { get; set; }

        public IAmmo Create() { throw new NotImplementedException(); }
    }

    public class AmmoTests
    {
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        [TestCase(0)]
        [TestCase(1)]
        public void InfiniteAmmo_CanAlwaysConsume(int amount)
        {
            IAmmo ammo = new InfiniteAmmo();
            Assert.IsTrue(ammo.CanConsume(amount), "Someone redefined infinity");

            ammo.Consume(amount);
            Assert.IsTrue(ammo.CanConsume(amount), "Consumed once, now infinity has a cap");
        }

        [Test]
        public void FixedAmmo_ConsumeAndDeplete()
        {
            IAmmo ammo = new FixedAmmo(10);

            Assert.IsTrue(ammo.CanConsume(5));
            Assert.IsTrue(ammo.CanConsume(10), "Should be able to consume all ammo");
            Assert.IsFalse(ammo.CanConsume(11), "Where did an extra bullet come from?");

            ammo.Consume(10);
            Assert.IsFalse(ammo.CanConsume(1), "Should not be able to consume any more ammo");
        }

        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        [TestCase(11)]
        [TestCase(-1)]
        [TestCase(0)]
        public void FixedAmmo_Overflow_Throws(int amount)
        {
            IAmmo ammo = new FixedAmmo(10);
            Assert.Throws<ArgumentOutOfRangeException>(() => ammo.Consume(amount));
        }

        [Test]
        public void RegeneratingAmmo_RegeneratesOverTime()
        {
            IAmmo ammo = new RegeneratingAmmo(new MockAmmoConfig
            {
                Type = AmmoType.Regenerating,
                DefaultAmount = 0,
                MaxAmount = 10,
                RegenerationDuration = 5,
                RegenerationDelay = 2
            });

            Assert.IsFalse(ammo.CanConsume(1),
                "Should not be able to consume any more ammo");
            
            // Tick for 1 second (should not regenerate yet)
            ammo.Tick(1);
            Assert.IsFalse(ammo.CanConsume(1),
                "Should not be able to consume any more ammo");
            
            // Tick for another 2 seconds (should start regenerating)
            ammo.Tick(2);
            Assert.IsTrue(ammo.CanConsume(1),
                "Should be able to consume some ammo after delay");
            
            // Tick for another 5 seconds (should fully regenerate)
            ammo.Tick(5);
            Assert.IsTrue(ammo.CanConsume(10),
                "Should be able to consume all ammo after full regeneration");
        }
    }
}
