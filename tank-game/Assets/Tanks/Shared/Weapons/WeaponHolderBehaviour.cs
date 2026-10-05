using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Messaging;
using Tanks.Shared.Stats;
using Tanks.Utils;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons
{
    public struct WeaponUsedFromServer : INetworkSerializeByMemcpy
    {
        public int ServerTick;
        public ShotId Id;
    }

    public struct WeaponUsed
    {
        public IWeaponHolderBehaviour Owner;
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

    public interface IWeaponHolderBehaviour : INetworkBehaviourExtended
    {
        IReadOnlyWeapon CurrentWeapon { get; }
        IReadOnlyWeapon DefaultWeapon { get; }
        ITransform MuzzlePoint { get; }
        IVelocity Velocity { get; }
        IEntityBehaviour Entity { get; }

        void SendInputServerRpc(WeaponInput input);
        void UseWeaponClientRpc(WeaponUsedFromServer message);

        float GetDeltaMultiplier();
        bool WeaponTick(float deltaTime, bool isHoldingDown, ShotId id);

        void SwitchTo(Required<Weapon> weapon);
        void SwitchToDefault();
        void SetDefault(Required<Weapon> weapon);
    }

    [RequireComponent(typeof(StatOwnerBehaviour), typeof(EntityBehaviour), typeof(Rigidbody2D))]
    public sealed class WeaponHolderBehaviour :
        NetworkBehaviourExtended, IWeaponHolderBehaviour, IMessageSender
    {
        [SerializeField] private Transform _muzzlePoint;

        public IReadOnlyWeapon CurrentWeapon => _currentWeapon;
        public IReadOnlyWeapon DefaultWeapon => _defaultWeapon;

        public ITransform MuzzlePoint { get; private set; }
        public IVelocity Velocity { get; private set; }
        public IEntityBehaviour Entity { get; private set; }

        [Inject] private readonly IMessageBus _bus;

        private Weapon _currentWeapon;
        private Weapon _defaultWeapon;
        private IStatOwnerBehaviour _statOwner;


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

        public float GetDeltaMultiplier()
        {
            float cooldown = _currentWeapon.Cooldown.DurationSeconds;

            // Avoid diving by zero
            if (cooldown <= math.EPSILON) return 1;

            float baseRateOfFire = 1 / cooldown;
            float rateOfFire = _statOwner.GetStat(StatType.RateOfFire, baseRateOfFire);

            return rateOfFire / baseRateOfFire;
        }

        public bool WeaponTick(float deltaTime, bool isHoldingDown, ShotId id)
        {
            if (deltaTime <= 0) return false;

            bool used = _currentWeapon.Tick(this, isHoldingDown, deltaTime, id);

            // Unless we want 2 ticks on our current weapon
            if (_defaultWeapon == _currentWeapon) return used;

            // Switch to default if all ammo is consumed
            if (_currentWeapon.Ammo.CanConsume() == false)
            {
                SwitchToDefault();
                return used;
            }
            
            _defaultWeapon.Tick(this, isHoldingDown: false, deltaTime, id);
            return used;
        }

        private void OnWeaponUsed(ShotId id)
        {
            WeaponUsed message = new WeaponUsed
            {
                Owner = this,
                Weapon = _currentWeapon,
                ShotId = id
            };

            EmitMessage(message);
            _bus.Publish(message, this);
        }

        protected override void Awake()
        {
            base.Awake();

            Velocity = new UnityVelocity(GetComponent<Rigidbody2D>());
            MuzzlePoint = new UnityTransform(_muzzlePoint);

            _statOwner = GetComponent<StatOwnerBehaviour>();
            Entity = GetComponent<EntityBehaviour>();
        }
    }
}
