using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Actions;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons
{
    public struct WeaponUsed
    {
        public Required<IReadOnlyWeapon> Weapon;
    }

    public sealed class WeaponHolderBehaviour : NetworkBehaviourExtended, IWeaponOwner
    {
        [SerializeField] private Transform _muzzlePoint;

        [Inject] private readonly IProjectilePoolService _projectilePool;

        public IReadOnlyWeapon CurrentWeapon => _currentWeapon;
        public IReadOnlyWeapon DefaultWeapon => _defaultWeapon;

        private Weapon _currentWeapon;
        private Weapon _defaultWeapon;


        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void StartUsingServerRpc() => StartUsingNotOwnerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void StopUsingServerRpc() => StopUsingNotOwnerRpc();


        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        public void StartUsingNotOwnerRpc() => StartUsing();

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        public void StopUsingNotOwnerRpc() => StopUsing();

        public void StartUsing() => _currentWeapon.StartUsing();
        public void StopUsing() => _currentWeapon.StopUsing();

        public void SwitchToDefault() => SwitchTo(_defaultWeapon);
        public void SwitchTo(Required<Weapon> weapon)
        {
            if (_currentWeapon != null) _currentWeapon.Used -= OnWeaponUsed;
            _currentWeapon = weapon;
            _currentWeapon.Used += OnWeaponUsed;
        }

        private void OnWeaponUsed()
        {
            EmitMessage(new WeaponUsed { Weapon = _currentWeapon });
        }

        private void Start()
        {
            _defaultWeapon = new Weapon();

            _defaultWeapon.Initialize(
                ammo: new RegeneratingAmmo(10, 10, 5f, 2.5f),
                cooldown: new Cooldown(0.2f),
                useMode: new FullAutomaticUseMode(),
                useAction: new ProjectileLaunchAction(
                    _projectilePool,
                    new UnityTransform(_muzzlePoint),
                    new NearFarDamage(24, 8, 2, 15)));

            SwitchToDefault();
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            _currentWeapon.Tick(deltaTime);

            // Unless we want 2 ticks on our current weapon
            if (_defaultWeapon != _currentWeapon) _defaultWeapon.Tick(deltaTime);
        }
    }
}