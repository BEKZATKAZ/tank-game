using NUnit.Framework;
using System;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Tests.Weapons
{
    class MockAmmoConfig : IAmmoConfig
    {
        public AmmoType Type { get; set; }

        public ushort DefaultAmount { get; set; }
        public ushort MaxAmount { get; set; }

        public float RegenerationDelay { get; set; }
        public float RegenerationDuration { get; set; }

        public IAmmo Create() { throw new NotImplementedException(); }
    }

    public class AmmoTests
    {
        [Test]
        public void RegeneratingAmmo_RegeneratesAtConstantRate()
        {
            IAmmo ammo = CreateRegeneratingAmmo();

            for (int i = 1; i <= 5; i++)
            {
                ammo.Tick(1);
                Assert.That(ammo.Current, Is.EqualTo(i * 2));
            }
        }

        [TestCase((ushort)5, 2.5f)]
        [TestCase((ushort)8, 1f)]
        public void RegeneratingAmmo_PartialMagazine_UsesFullCapacityRate(ushort initial, float duration)
        {
            IAmmo ammo = CreateRegeneratingAmmo(initial);

            ammo.Tick(duration - 0.125f);
            Assert.That(ammo.Current, Is.EqualTo(9));

            ammo.Tick(0.125f);
            Assert.That(ammo.Current, Is.EqualTo(10));
        }

        [Test]
        public void RegeneratingAmmo_SmallTicks_PreserveFractionalProgress()
        {
            IAmmo smallTicks = CreateRegeneratingAmmo(delay: 2);
            IAmmo largeTick = CreateRegeneratingAmmo(delay: 2);

            for (int i = 0; i < 56; i++)
            {
                smallTicks.Tick(0.125f);
            }

            largeTick.Tick(7);

            Assert.That(smallTicks.Current, Is.EqualTo(10));
            Assert.That(smallTicks.Current, Is.EqualTo(largeTick.Current));
        }

        [Test]
        public void RegeneratingAmmo_DelayCrossing_OnlyRegeneratesTimeAfterDelay()
        {
            IAmmo ammo = CreateRegeneratingAmmo(delay: 2);

            ammo.Tick(2);
            Assert.That(ammo.Current, Is.Zero);

            ammo.Tick(0.25f);
            Assert.That(ammo.Current, Is.Zero);

            ammo.Tick(0.25f);
            Assert.That(ammo.Current, Is.EqualTo(1));

            IAmmo crossing = CreateRegeneratingAmmo(delay: 2);
            crossing.Tick(2.5f);
            Assert.That(crossing.Current, Is.EqualTo(ammo.Current));
        }

        [Test]
        public void RegeneratingAmmo_Consume_ResetsDelayAndFractionalProgress()
        {
            IAmmo ammo = CreateRegeneratingAmmo(5, 2);

            ammo.Tick(2.25f);
            ammo.Consume();
            Assert.That(ammo.Current, Is.EqualTo(4));

            ammo.Tick(2);
            Assert.That(ammo.Current, Is.EqualTo(4));

            ammo.Tick(0.25f);
            Assert.That(ammo.Current, Is.EqualTo(4));

            ammo.Tick(0.25f);
            Assert.That(ammo.Current, Is.EqualTo(5));
        }

        [TestCase((ushort)10)]
        [TestCase(ushort.MaxValue)]
        public void RegeneratingAmmo_LargeTick_ClampsAtMaximum(ushort maximum)
        {
            IAmmo ammo = new RegeneratingAmmo(new MockAmmoConfig
            {
                MaxAmount = maximum,
                RegenerationDuration = 5
            });

            ammo.Tick(1000);
            Assert.That(ammo.Current, Is.EqualTo(maximum));

            ammo.Tick(1000);
            ammo.Consume();
            ammo.Tick(0);
            Assert.That(ammo.Current, Is.EqualTo(maximum - 1));
        }

        private static IAmmo CreateRegeneratingAmmo(ushort initial = 0, float delay = 0)
        {
            return new RegeneratingAmmo(new MockAmmoConfig
            {
                Type = AmmoType.Regenerating,
                DefaultAmount = initial,
                MaxAmount = 10,
                RegenerationDuration = 5,
                RegenerationDelay = delay
            });
        }

        [TestCase(ushort.MaxValue)]
        [TestCase((ushort)0)]
        [TestCase((ushort)1)]
        public void InfiniteAmmo_CanAlwaysConsume(ushort amount)
        {
            IAmmo ammo = new InfiniteAmmo();
            Assert.True(ammo.CanConsume(amount), "Someone redefined infinity");

            ammo.Consume(amount);
            ammo.Tick(0.1f);
            Assert.True(ammo.CanConsume(amount), "Consumed once, now infinity has a cap");
        }

        [Test]
        public void FixedAmmo_ConsumeAndDeplete()
        {
            IAmmo ammo = new FixedAmmo(10);

            Assert.True(ammo.CanConsume(5));
            Assert.True(ammo.CanConsume(10), "Should be able to consume all ammo");
            Assert.False(ammo.CanConsume(11), "Where did an extra bullet come from?");

            ammo.Consume(10);
            Assert.False(ammo.CanConsume(1), "Should not be able to consume any more ammo");
        }

        [TestCase(ushort.MaxValue)]
        [TestCase((ushort)1000)]
        [TestCase((ushort)11)]
        [TestCase((ushort)0)]
        public void FixedAmmo_Overflow_Throws(ushort amount)
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

            Assert.False(ammo.CanConsume(1),
                "Should not be able to consume any more ammo");
            
            // Tick for 1 second (should not regenerate yet)
            ammo.Tick(1);
            Assert.False(ammo.CanConsume(1),
                "Should not be able to consume any more ammo");
            
            // Tick for another 2 seconds (should start regenerating)
            ammo.Tick(2);
            Assert.True(ammo.CanConsume(1),
                "Should be able to consume some ammo after delay");
            
            // Tick for another 5 seconds (should fully regenerate)
            ammo.Tick(5);
            Assert.True(ammo.CanConsume(10),
                "Should be able to consume all ammo after full regeneration");

            ammo.Consume(10);
            Assert.False(ammo.CanConsume(1),
                "Should not be able to consume any more ammo as already consumed");
        }
    }
}
