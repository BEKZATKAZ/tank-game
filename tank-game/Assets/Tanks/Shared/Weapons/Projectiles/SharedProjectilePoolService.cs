using System.Collections.Generic;
using Tanks.Shared.Weapons.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Tanks.Shared.Weapons.Projectiles
{
    public sealed class SharedProjectilePoolService :
        NetworkBehaviourExtended, IProjectilePoolService
    {
        private IObjectResolver _container;
        private ProjectilePoolLogic _logic;

        [Inject]
        public void Construct(IObjectResolver container)
        {
            _container = container;
            _logic = new ProjectilePoolLogic(_container);
        }

        [Rpc(SendTo.NotServer,
            Delivery = RpcDelivery.Unreliable,
            InvokePermission = RpcInvokePermission.Server)]
        private void ReconcileProjectileClientRpc(
            ProjectileId id, ProjectileSnapshotFromServer snapshot)
        {
            if (_logic.TryGetById(id, out IProjectileBehaviour projectile) == false)
            {
                // Projectiles may already be released by the time RPC arrives
                return;
            }

            projectile.Reconcile(snapshot);
        }

        [Rpc(SendTo.Server)]
        private void RequestReturnServerRpc(ProjectileId id)
        {
            EmitMessage(new ProjectileReturnRequested { Id = id });
        }

        public bool TryGetProjectile(ProjectileId id, out IProjectileBehaviour instance)
        {
            return _logic.TryGetById(id, out instance);
        }

        public void RequestReconciliation(
            IProjectileBehaviour projectile, ProjectileSnapshotFromServer snapshot)
        {
            if (_logic.TryGetByInstance(projectile, out ProjectileId id) == false) return;
            ReconcileProjectileClientRpc(id, snapshot);
        }

        public bool TryInitializePool(IProjectileConfig config)
        {
            return _logic.TryInitializePool(config);
        }

        public bool TryRent(
            IProjectileConfig config, ProjectileId id, out IProjectileBehaviour instance)
        {
            instance = null;

            if (_logic.TryGetById(id, out _))
            {
                Debug.LogWarning("Duplicate projectile detected: " + id);
                return false;
            }

            if (_logic.TryGetPool(config, out ObjectPool<ProjectileBehaviour> pool) == false)
            {
                Debug.LogWarning("No pool found for config: " + config);
                return false;
            }

            instance = pool.Get();
            _logic.Set(id, instance);
            return true;
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void ReturnProjectileClientRpc(ProjectileId id)
        {
            if (_logic.TryGetById(id, out IProjectileBehaviour projectile) == false) return;
            Return(projectile);
        }

        public void RequestReturnAsClient(IProjectileBehaviour instance)
        {
            if (_logic.TryGetByInstance(instance, out ProjectileId id) == false) return;
            RequestReturnServerRpc(id);
            EmitMessage(new ProjectileReturnRequested { Id = id, Instance = instance });
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void RejectReturnClientRpc(ProjectileReturnRequestRejected message)
        {
            EmitMessage(message);
        }

        public void Return(IProjectileBehaviour instance, bool removeHandle = true)
        {
            if (_logic.TryGetPool(instance.Config, out var pool) == false)
            {
                Debug.LogWarning("Projectile pool not found for config: " + instance.Config);
                return;
            }

            pool.Release(instance as ProjectileBehaviour);
            if (removeHandle == false) return;

            ProjectileId id = _logic.RemoveByInstance(instance);
            EmitMessage(new ProjectileReturned { Id = id });
        }

        public void ReturnAll()
        {
            IEnumerable<IProjectileBehaviour> projectiles = _logic.GetActiveProjectiles();

            foreach (IProjectileBehaviour projectile in projectiles)
            {
                Return(projectile, false);
            }

            _logic.Clear();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _logic.Dispose();
        }
    }
}
