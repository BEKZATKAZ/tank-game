using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Stats;
using Tanks.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Weapons
{
    public struct WeaponUsedFromServer : INetworkSerializeByMemcpy
    {
        public ShotId Id;
    }

    public struct WeaponUsed
    {
        public Required<IReadOnlyWeapon> Weapon;
        public ShotId ShotId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ClientShot
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClientShot(int tick, ShotId id)
        {
            ClientTick = tick;
            Id = id;
        }

        public readonly int ClientTick;
        public readonly ShotId Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WeaponInput : INetworkSerializeByMemcpy
    {
        public readonly int ClientTick => FireBuffer[^1].ClientTick;

        public RingBuffer8<ClientShot> FireBuffer;
    }

    [RequireComponent(typeof(StatOwnerBehaviour))]
    public sealed class WeaponHolderBehaviour : NetworkBehaviourExtended, IWeaponOwner
    {
        [SerializeField] private Transform _muzzlePoint;

        public IReadOnlyWeapon CurrentWeapon => _currentWeapon;
        public IReadOnlyWeapon DefaultWeapon => _defaultWeapon;
        public ITransform MuzzlePoint { get; private set; }
        public IStatOwner EffectOwner { get; private set; }

        private Weapon _currentWeapon;
        private Weapon _defaultWeapon;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SendInputServerRpc(WeaponInput input) => EmitMessage(input);

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void UseWeaponClientRpc(WeaponUsedFromServer message) => EmitMessage(message);

        public void SwitchToDefault() => SwitchTo(_defaultWeapon);
        public void SetDefault(Required<Weapon> weapon) => _defaultWeapon = weapon;

        public void SwitchTo(Required<Weapon> weapon)
        {
            if (_currentWeapon == weapon) return;
            if (_currentWeapon != null) _currentWeapon.Used -= OnWeaponUsed;

            _currentWeapon = weapon;
            _currentWeapon.Used += OnWeaponUsed;
        }

        public bool WeaponTick(float deltaTime, bool isHoldingDown, ShotId id)
        {
            bool used = _currentWeapon.Tick(this, isHoldingDown, deltaTime, id);

            // Unless we want 2 ticks on our current weapon
            if (_defaultWeapon == _currentWeapon) return used;

            // Switch to default if all ammo is consumed
            if (_currentWeapon.Ammo.CanConsume() == false)
            {
                SwitchToDefault();
            }
            
            _defaultWeapon.Tick(this, isHoldingDown: false, deltaTime, id);
            return used;
        }

        private void OnWeaponUsed(ShotId id)
        {
            EmitMessage(new WeaponUsed { Weapon = _currentWeapon, ShotId = id });
        }

        protected override void Awake()
        {
            base.Awake();
            MuzzlePoint = new UnityTransform(_muzzlePoint);
            EffectOwner = GetComponent<StatOwnerBehaviour>();
        }
    }
}
