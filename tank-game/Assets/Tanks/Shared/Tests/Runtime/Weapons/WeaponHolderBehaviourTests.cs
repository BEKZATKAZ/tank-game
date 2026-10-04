using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Helpers;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using UnityEngine;

namespace Tanks.Shared.Tests.Runtime.Weapons
{
    public class WeaponHolderBehaviourTests
    {
        private WeaponHolderBehaviour _behaviour;
        private StatOwnerBehaviour _statOwner;

        [SetUp]
        public void Setup()
        {
            Weapon weapon = new Weapon();

            IAmmoConfig ammoConfig = Substitute.For<IAmmoConfig>();
            ammoConfig.DefaultAmount.Returns(10);
            ammoConfig.MaxAmount.Returns(10);
            ammoConfig.RegenerationDelay.Returns(2);
            ammoConfig.RegenerationDuration.Returns(5);

            IRecoilConfig recoilConfig = Substitute.For<IRecoilConfig>();
            recoilConfig.DurationSeconds.Returns(1);
            recoilConfig.Amount.Returns(10);

            weapon.Initialize("default",
                new RegeneratingAmmo(ammoConfig),
                new Cooldown(0.2f),
                new FullAutomaticUseMode(),
                new NoUseAction(),
                recoilConfig.AsRequired());

            _behaviour = new GameObject().AddComponent<WeaponHolderBehaviour>();
            _statOwner = _behaviour.GetComponent<StatOwnerBehaviour>();

            _statOwner.Config = ScriptableObject.CreateInstance<StatTypeParametersConfig>();
            _statOwner.Config.Parameters[StatType.RateOfFire] = new StatTypeParameters
            {
                MinValue = 0,
                MaxValue = 20,
                DiminishingBase = 0
            };

            _behaviour.SetDefault(weapon);
            _behaviour.SwitchTo(weapon);
        }

        [TearDown]
        public void Teardown()
        {
            if (_statOwner.Config) Object.DestroyImmediate(_statOwner.Config);
            if (_behaviour) Object.DestroyImmediate(_behaviour.gameObject);
        }

        [Test]
        public void GetDeltaMultiplier_CalculatesRateOfFire()
        {
            float baseRateOfFire = 1f / _behaviour.CurrentWeapon.Cooldown.DurationSeconds;

            Assert.That(_behaviour.GetDeltaMultiplier(), Is.EqualTo(1));

            // +50%
            _statOwner.AddStat(StatType.RateOfFire, StatValueType.Percent, 50);

            Assert.That(_behaviour.GetDeltaMultiplier(), Is.EqualTo(1.5f));

            // +1.5
            _statOwner.AddStat(StatType.RateOfFire, StatValueType.Flat, 1.5f);

            float stat = _statOwner.GetStat(StatType.RateOfFire, baseRateOfFire);
            float expected = stat / baseRateOfFire;

            Assert.That(_behaviour.GetDeltaMultiplier(), Is.EqualTo(expected));
        }
    }
}
