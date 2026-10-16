using System.Buffers;
using System.Collections.Generic;
using Tanks.Shared.Generation;
using Tanks.Shared.Items.Data;
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
        public ItemIdentifier ItemId;
        public float2 WorldPosition;
    }

    public interface IPickupSpawnBehaviour : INetworkBehaviourExtended
    {
        IReadOnlyDictionary<ItemTier, int> ItemCounts { get; }

        void SpawnEveryoneRpc(PickupSpawned payload);
        void DespawnEveryoneRpc(PickupId id);
    }

    public sealed class PickupSpawnBehaviour :
        NetworkBehaviourExtended, IPickupSpawnBehaviour, IMessageHandler<TilemapGenerated>
    {
        [SerializeField] private Dictionary<ItemTier, int> _itemCounts;

        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IObjectResolver _container;

        private readonly Dictionary<PickupId, PickupBehaviour> _pickups = new();
        private ObjectPool<PickupBehaviour> _pool;
        private PickupId _lastPickupId;

        public IReadOnlyDictionary<ItemTier, int> ItemCounts => _itemCounts;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SpawnEveryoneRpc(PickupSpawned payload)
        {
            _lastPickupId = new PickupId(_lastPickupId.Value + 1);

            PickupBehaviour instance = _pool.Get();
            _pickups[_lastPickupId] = instance;

            instance.Transform.WorldPosition = payload.WorldPosition;
            instance.Initialize(payload, _lastPickupId, this);
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
            if (_pool is null) return;

            _bus.UnregisterHandler<TilemapGenerated>(this);
            _pool.Dispose();
            _pool = null;
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
