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
using UnityEngine.UIElements;

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

        [Test]
        public void Update_TimersChangedWithoutStatChange_RefreshesInExpiryOrder()
        {
            IEntityBehaviour player = CreatePossessedEntity();
            IStatOwnerBehaviour statOwner = player.GetBehaviour<IStatOwnerBehaviour>();
            List<StatTimer> timers = new()
            {
                new StatTimer(new StatId(1), 28),
                new StatTimer(new StatId(2), 12)
            };

            statOwner.When(owner => owner.GetStatTimers(StatType.Speed, Arg.Any<List<StatTimer>>()))
                .Do(args => args.Arg<List<StatTimer>>().AddRange(timers));

            ViewModel.Update();

            int updates = 0;
            ViewModel.Stats.Bind(_ => updates++);
            updates = 0;

            timers[1] = new StatTimer(new StatId(2), 11);
            ViewModel.Update();

            StatChanges speed = ViewModel.Stats.Value[StatType.Speed];

            Assert.That(updates, Is.EqualTo(1));
            Assert.That(speed.CurrentValue, Is.EqualTo(10));
            Assert.That(speed.Change, Is.Zero);
            Assert.That(speed.Timers[0].Id, Is.EqualTo(new StatId(2)));
            Assert.That(speed.Timers[0].RemainingSeconds, Is.EqualTo(11));
            Assert.That(speed.Timers[1].RemainingSeconds, Is.EqualTo(28));

            ViewModel.Update();

            Assert.That(updates, Is.EqualTo(1));

            timers[0] = new StatTimer(new StatId(1), 27.9f);
            ViewModel.Update();

            Assert.That(updates, Is.EqualTo(1));

            timers.Clear();
            ViewModel.Update();

            Assert.That(updates, Is.EqualTo(2));
            Assert.That(speed.Timers, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnPossessed_ChangedOrLost_ClearsTimersImmediately(bool lost)
        {
            IClientEntityService service = Substitute.For<IClientEntityService>();
            using PlayerStatsViewModel viewModel = new(service);
            IEntityBehaviour previous = CreatePossessedEntity();

            viewModel.Stats.Value[StatType.Speed].Timers.Add(new StatTimer(new StatId(1), 10));

            int updates = 0;
            viewModel.Stats.Bind(_ => updates++);
            updates = 0;

            service.Changed += Raise.Event<PossessedEntityChanged>(previous, lost ? null : CreatePossessedEntity());

            Assert.That(viewModel.Stats.Value[StatType.Speed].Timers, Is.Empty);
            Assert.That(updates, Is.EqualTo(1));
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
