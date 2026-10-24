using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.UI.Gameplay.PlayerStats;

namespace Tanks.UI.Tests.Gameplay
{
    public class PlayerAmmoTests : ViewModelTestBase<PlayerAmmoViewModel>
    {
        private IClientEntityService _clientEntityService;
        private IEntityBehaviour _player;
        private IWeaponHolderBehaviour _weaponHolder;

        protected override PlayerAmmoViewModel CreateViewModel()
        {
            _clientEntityService = Substitute.For<IClientEntityService>();

            _player = Substitute.For<IEntityBehaviour>();
            _player.IsValid.Returns(true);

            _weaponHolder = _player.GetBehaviour<IWeaponHolderBehaviour>();
            _clientEntityService.GetPossessedEntity().Returns(_player);

            SetAmmo(_weaponHolder, new FixedAmmo(5));

            return new PlayerAmmoViewModel(_clientEntityService);
        }

        [Test]
        public void Initialize_AlreadyPossessed_ReadsAmmo()
        {
            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)5, (ushort)5)));
        }

        [Test]
        public void Update_AmmoConsumed_UpdatesCurrent()
        {
            FixedAmmo ammo = new FixedAmmo(5);
            SetAmmo(_weaponHolder, ammo);

            ammo.Consume(2);
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)3, (ushort)5)));
        }

        [Test]
        public void Update_AmmoRegenerated_UpdatesCurrent()
        {
            IAmmoConfig config = Substitute.For<IAmmoConfig>();
            config.DefaultAmount.Returns((ushort)5);
            config.MaxAmount.Returns((ushort)5);
            config.RegenerationDuration.Returns(1);

            RegeneratingAmmo ammo = new RegeneratingAmmo(config);
            SetAmmo(_weaponHolder, ammo);

            ammo.Consume(2);
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value.Current, Is.EqualTo(3));

            ammo.Tick(2);
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value.Current, Is.EqualTo(5));
        }

        [Test]
        public void Update_WeaponSwitched_UpdatesCapacity()
        {
            SetAmmo(_weaponHolder, new FixedAmmo(8));
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)8, (ushort)8)));
        }

        [Test]
        public void PossessionChanged_ReadsNewPlayer()
        {
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            next.IsValid.Returns(true);

            SetAmmo(next.GetBehaviour<IWeaponHolderBehaviour>(), new FixedAmmo(2));

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, next);

            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)2, (ushort)2)));
        }

        [Test]
        public void PossessionLost_ClearsAmmo()
        {
            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, null);

            Assert.That(ViewModel.Ammo.Value.Max, Is.Zero);
        }

        [Test]
        public void Update_InvalidPlayer_ClearsAmmo()
        {
            _player.IsValid.Returns(false);
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value.Max, Is.Zero);
        }

        [Test]
        public void Update_MissingWeapon_ClearsAmmo()
        {
            _weaponHolder.CurrentWeapon.Returns((IReadOnlyWeapon)null);
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value.Max, Is.Zero);
        }

        [Test]
        public void Update_EmptyMagazine_KeepsCapacity()
        {
            FixedAmmo ammo = new FixedAmmo(5);
            ammo.Consume(5);
            SetAmmo(_weaponHolder, ammo);

            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)0, (ushort)5)));
        }

        [Test]
        public void Update_InfiniteAmmo_ShowsOneSlot()
        {
            SetAmmo(_weaponHolder, new InfiniteAmmo());
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value, Is.EqualTo(((ushort)1, (ushort)1)));
        }

        [Test]
        public void Update_ZeroCapacity_ClearsAmmo()
        {
            SetAmmo(_weaponHolder, new FixedAmmo(0));
            ViewModel.Update();

            Assert.That(ViewModel.Ammo.Value.Max, Is.Zero);
        }

        [Test]
        public void Update_UnchangedAmmo_DoesNotNotifyAgain()
        {
            int notifications = 0;
            ViewModel.Ammo.Bind(_ => notifications++);

            ViewModel.Update();
            ViewModel.Update();

            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_UnsubscribesFromPossession()
        {
            ViewModel.Dispose();

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, null);

            Assert.That(ViewModel.Ammo.Value.Max, Is.EqualTo(5));
        }

        private static void SetAmmo(IWeaponHolderBehaviour holder, IReadOnlyAmmo ammo)
        {
            IReadOnlyWeapon weapon = Substitute.For<IReadOnlyWeapon>();
            weapon.Ammo.Returns(ammo);

            holder.CurrentWeapon.Returns(weapon);
        }
    }
}
