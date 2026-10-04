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

        public bool TryInitializePool(ProjectileBehaviour prefab)
        {
            return _logic.TryInitializePool(prefab);
        }

        public bool TryRent(string name, ProjectileId id, out IProjectileBehaviour instance)
        {
            instance = null;

            if (_logic.TryGetById(id, out _))
            {
                Debug.LogWarning("Duplicate projectile detected: " + id);
                return false;
            }

            if (_logic.TryGetPool(name, out ObjectPool<ProjectileBehaviour> pool) == false)
            {
                Debug.LogWarningFormat("No pool found '{0}'", name);
                return false;
            }

            instance = pool.Get();
            _logic.Set(id, instance);
            return true;
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void ReturnProjectileClientRpc(ProjectileId id)
        {
            Return(id);
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

        public void Return(ProjectileId id, bool removeHandle = true)
        {
            if (_logic.TryGetById(id, out IProjectileBehaviour instance) == false) return;

            if (_logic.TryGetPool(instance.Name, out var pool) == false)
            {
                Debug.LogWarningFormat("Projectile pool not found '{0}'", instance.Name);
                return;
            }

            pool.Release(instance as ProjectileBehaviour);
            if (removeHandle == false) return;

            _logic.RemoveByInstance(instance);
            EmitMessage(new ProjectileReturned { Id = id });
        }

        public void ReturnAll()
        {
            IEnumerable<IProjectileBehaviour> projectiles = _logic.GetActiveProjectiles();

            foreach (IProjectileBehaviour projectile in projectiles)
            {
                Return(projectile.Id, false);
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
