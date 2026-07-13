using System.Buffers;
using System.Collections.Generic;
using Tanks.Shared.Weapons.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace Tanks.Shared.Weapons.Projectiles
{
    public sealed class SharedProjectilePoolService :
        NetworkBehaviourExtended, IProjectilePoolService
    {
        public IEnumerable<KeyValuePair<ProjectileId, IProjectile>> ActiveProjectiles => _idToProjectiles;

        private readonly Dictionary<IProjectileConfig, ObjectPool<ProjectileBehaviour>> _pools = new();
        private readonly Dictionary<ProjectileId, IProjectile> _idToProjectiles = new();
        private readonly Dictionary<IProjectile, ProjectileId> _projectilesToId = new();

        private void DestroyProjectile(ProjectileBehaviour instance)
        {
            if (instance) Destroy(instance.gameObject);
        }

        [Rpc(SendTo.NotServer,
            Delivery = RpcDelivery.Unreliable,
            InvokePermission = RpcInvokePermission.Server)]
        private void ReconcileProjectileClientRpc(
            ProjectileId id, ProjectileSnapshotFromServer snapshot)
        {
            if (_idToProjectiles.TryGetValue(id, out IProjectile projectile) == false)
            {
                // Projectiles may already be released by the time RPC arrives
                return;
            }

            projectile.Reconcile(snapshot);
        }

        public void RequestReconciliation(
            IProjectile projectile, ProjectileSnapshotFromServer snapshot)
        {
            if (_projectilesToId.TryGetValue(projectile, out ProjectileId id) == false) return;
            ReconcileProjectileClientRpc(id, snapshot);
        }

        public bool TryInitializePool(IProjectileConfig config)
        {
            if (_pools.ContainsKey(config)) return false;

            int initialSize = 25;
            int maxSize = 1000;

            ObjectPool<ProjectileBehaviour> pool = new ObjectPool<ProjectileBehaviour>(
                createFunc: () => config.Create() as ProjectileBehaviour,
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: DestroyProjectile,
                collectionCheck: true,
                initialSize,
                maxSize);

            // Prevent stutters during gameplay
            ProjectileBehaviour[] warmup = ArrayPool<ProjectileBehaviour>.Shared.Rent(initialSize);

            try
            {
                for (int i = 0; i < initialSize; i++) warmup[i] = pool.Get();
                for (int i = 0; i < initialSize; i++) pool.Release(warmup[i]);
            }
            finally
            {
                ArrayPool<ProjectileBehaviour>.Shared.Return(warmup);
                _pools[config] = pool;
            }

            return true;
        }

        public bool TryRent(IProjectileConfig config, ProjectileId id, out IProjectile instance)
        {
            if (_idToProjectiles.ContainsKey(id))
            {
                Debug.LogWarning("Duplicate projectile detected");
                instance = null;
                return false;
            }

            instance = _pools[config].Get();
            _projectilesToId[instance] = id;
            _idToProjectiles[id] = instance;

            return true;
        }

        public void Return(IProjectile instance, bool removeHandle = true)
        {
            _pools[instance.Config].Release(instance as ProjectileBehaviour);
            if (removeHandle == false) return;

            _projectilesToId.Remove(instance, out ProjectileId id);
            _idToProjectiles.Remove(id);
        }

        public void ReturnAll()
        {
            foreach (IProjectile projectile in _projectilesToId.Keys)
            {
                Return(projectile, removeHandle: false);
            }

            _projectilesToId.Clear();
            _idToProjectiles.Clear();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var pool in _pools.Values) pool.Dispose();
        }
    }
}
