using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Core.Helpers;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.UI.Gameplay.PlayerStats;
using Unity.Mathematics;

namespace Tanks.UI.Tests.Gameplay
{
    public class PlayerStatsTests : ViewModelTestBase<PlayerStatsViewModel>
    {
        private IClientEntityService _clientEntityService;

        protected override PlayerStatsViewModel CreateViewModel()
        {
            _clientEntityService = new ClientEntityService();
            return new PlayerStatsViewModel(_clientEntityService);
        }

        [Test]
        public void Update_NoPossessedPlayer_DoesNothing()
        {
            int statsUpdatedCount = -1;

            void OnStatsUpdated(IDictionary<StatType, StatChanges> stats)
            {
                statsUpdatedCount++;
            }

            ViewModel.Stats.Bind(OnStatsUpdated);
            ViewModel.Update();
            Assert.That(statsUpdatedCount, Is.EqualTo(0));

            CreatePossessedEntity();
            Assert.That(statsUpdatedCount, Is.EqualTo(0));

            ViewModel.Update();
            Assert.That(statsUpdatedCount, Is.EqualTo(1));
        }

        [Test]
        public void Update_StatAdded_UpdatesChangeAndCurrent()
        {
            IEntityBehaviour player = CreatePossessedEntity();
            IStatOwnerBehaviour statOwner = player.GetBehaviour<IStatOwnerBehaviour>();
            IWeaponHolderBehaviour weaponOwner = player.GetBehaviour<IWeaponHolderBehaviour>();

            ViewModel.Update();

            float baseSpeed = player.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed;
            float weaponCooldown = weaponOwner.CurrentWeapon.Cooldown.DurationSeconds;
            float baseRateOfFire = weaponCooldown <= math.EPSILON ? 0 : 1f / weaponCooldown;

            // +50%
            statOwner.GetStat(StatType.Speed, baseSpeed).Returns(baseSpeed * 1.5f);

            // -10
            statOwner.GetStat(StatType.Defense, 0).Returns(-10);

            // +100%
            statOwner.GetStat(StatType.RateOfFire, baseRateOfFire).Returns(baseRateOfFire * 2);

            ViewModel.Update();

            AssertStat(StatType.Speed, baseSpeed, baseSpeed * 1.5f);
            AssertStat(StatType.Defense, 0, -10);
            AssertStat(StatType.RateOfFire, baseRateOfFire, baseRateOfFire * 2);
        }

        private void AssertStat(StatType type, float previous, float current)
        {
            StatChanges stat = ViewModel.Stats.Value[type];

            Assert.That(stat.CurrentValue, Is.EqualTo(current),
                $"Current value of '{type}' is incorrect");

            Assert.That(stat.Change, Is.EqualTo(current - previous),
                $"Change  of '{type}' is incorrect");
        }

        private IEntityBehaviour CreatePossessedEntity()
        {
            IEntityBehaviour player = Substitute.For<IEntityBehaviour>();
            IWeaponHolderBehaviour weaponOwner = player.GetBehaviour<IWeaponHolderBehaviour>();
            IStatOwnerBehaviour statOwner = player.GetBehaviour<IStatOwnerBehaviour>();
            ITankDriveBehaviour driveBehaviour = player.GetBehaviour<ITankDriveBehaviour>();

            // Create current weapon
            IReadOnlyWeapon weapon = Substitute.For<IReadOnlyWeapon>();
            weapon.Cooldown.DurationSeconds.Returns(0.25f);
            weaponOwner.CurrentWeapon.Returns(weapon);

            // Returns base value
            statOwner.GetStat(Arg.Any<StatType>(), Arg.Any<float>())
                .Returns(args => (float)args[1]);

            driveBehaviour.DefaultSpeed.Returns(10);

            _clientEntityService.PossessEntity(player.AsRequired());
            return player;
        }
    }
}
