using NaughtyAttributes;
using System.Buffers;
using System.Collections.Generic;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Items.Pickups
{
    public struct PickupSpawned : INetworkSerializeByMemcpy
    {
        public PickupId PickupId;
        public ItemIdentifier ItemId;
        public float2 WorldPosition;
    }

    public interface IPickupSpawnBehaviour : INetworkBehaviourExtended
    {
        float MinCooldown { get; }
        float MaxCooldown { get; }
        float MinDistanceToPlayers { get; }

        void SpawnEveryoneRpc(PickupSpawned payload);
        void DespawnEveryoneRpc(PickupId id);
    }

    public sealed class PickupSpawnBehaviour :
        NetworkBehaviourExtended, IPickupSpawnBehaviour, IMessageHandler<TilemapGenerated>
    {
        [MinMaxSlider(0, 120)]
        [SerializeField]
        private Vector2 _cooldownMinMax;

        [Range(1, 5)]
        [SerializeField]
        private float _minDistanceToPlayers = 2;

        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IObjectResolver _container;

        private readonly Dictionary<PickupId, PickupBehaviour> _pickups = new();
        private ObjectPool<PickupBehaviour> _pool;

        public float MinCooldown => _cooldownMinMax.x;
        public float MaxCooldown => _cooldownMinMax.y;
        public float MinDistanceToPlayers => _minDistanceToPlayers;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SpawnEveryoneRpc(PickupSpawned payload)
        {
            PickupBehaviour instance = _pool.Get();
            _pickups[payload.PickupId] = instance;

            instance.Transform.WorldPosition = payload.WorldPosition;
            instance.Initialize(payload, this);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void DespawnEveryoneRpc(PickupId id)
        {
            if (_pickups.Remove(id, out PickupBehaviour instance) == false) return;
            _pool.Release(instance);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            // Must reset each new round
            foreach (PickupBehaviour pickup in _pickups.Values) _pool.Release(pickup);
            _pickups.Clear();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _pool?.Dispose();
        }

        protected override void OnInitialize()
        {
            _pool = new ObjectPool<PickupBehaviour>(
                createFunc: CreatePickup,
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: DestroyPickup
            );

            // Prevent stutters during gameplay
            int initialSize = 10;
            PickupBehaviour[] warmup = ArrayPool<PickupBehaviour>.Shared.Rent(initialSize);

            try
            {
                for (int i = 0; i < initialSize; i++) warmup[i] = _pool.Get();
                for (int i = 0; i < initialSize; i++) _pool.Release(warmup[i]);
            }
            finally
            {
                ArrayPool<PickupBehaviour>.Shared.Return(warmup);
            }

            _bus.RegisterHandler<TilemapGenerated>(this);
        }

        private PickupBehaviour CreatePickup()
        {
            PickupBehaviour instance = new GameObject("pickup").AddComponent<PickupBehaviour>();
            _container.InjectGameObject(instance.gameObject);
            return instance;
        }

        private void DestroyPickup(PickupBehaviour instance)
        {
            if (instance) Destroy(instance);
        }
    }
}