using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Actions;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons
{
    public sealed class WeaponHolder : NetworkBehaviourExtended, IWeaponOwner
    {
        [SerializeField] private Transform _muzzlePoint;

        [Inject] private readonly IProjectilePoolService _projectilePool;

        public IReadOnlyWeapon CurrentWeapon => _currentWeapon;
        public IReadOnlyWeapon DefaultWeapon => _defaultWeapon;

        private Weapon _currentWeapon;
        private Weapon _defaultWeapon;


        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void StartUsingServerRpc() => StartUsing();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void StopUsingServerRpc() => StopUsing();

        public void StartUsing() => _currentWeapon.StartUsing();
        public void StopUsing() => _currentWeapon.StopUsing();

        public void SwitchTo(Weapon weapon) => _currentWeapon = weapon;
        public void SwitchToDefault() => _currentWeapon = _defaultWeapon;

        private void Start()
        {
            _defaultWeapon = new Weapon();

            _defaultWeapon.Initialize(
                ammo: new RegeneratingAmmo(10, 10, 5f, 2.5f),
                cooldown: new Cooldown(0.2f),
                useMode: new FullAutomaticUseMode(),
                useAction: new ProjectileLaunchAction(
                    _projectilePool, new UnityTransform(_muzzlePoint)));

            _currentWeapon = _defaultWeapon;
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