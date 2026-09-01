using System.Buffers;
using System.Collections.Generic;
using Tanks.Shared.Weapons.Data;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Weapons.Projectiles
{
    public sealed class ProjectilePoolLogic : System.IDisposable
    {
        public ProjectilePoolLogic(IObjectResolver container)
        {
            _container = container;
        }

        private readonly Dictionary<IProjectileConfig, ObjectPool<ProjectileBehaviour>> _pools = new();
        private readonly Dictionary<ProjectileId, IProjectileBehaviour> _idToProjectiles = new();
        private readonly Dictionary<IProjectileBehaviour, ProjectileId> _projectilesToId = new();
        private readonly IObjectResolver _container;

        public void Dispose()
        {
            foreach (var pool in _pools.Values) pool.Dispose();
        }

        public IEnumerable<IProjectileBehaviour> GetActiveProjectiles()
        {
            return _idToProjectiles.Values;
        }

        public void Clear()
        {
            _idToProjectiles.Clear();
            _projectilesToId.Clear();
        }

        public bool TryGetPool(IProjectileConfig config, out ObjectPool<ProjectileBehaviour> pool)
        {
            return _pools.TryGetValue(config, out pool);
        }

        public bool TryGetById(ProjectileId id, out IProjectileBehaviour instance)
        {
            return _idToProjectiles.TryGetValue(id, out instance);
        }

        public bool TryGetByInstance(IProjectileBehaviour instance, out ProjectileId id)
        {
            return _projectilesToId.TryGetValue(instance, out id);
        }

        public void Set(ProjectileId id, IProjectileBehaviour instance)
        {
            _idToProjectiles[id] = instance;
            _projectilesToId[instance] = id;
        }

        public ProjectileId RemoveByInstance(IProjectileBehaviour instance)
        {
            _projectilesToId.Remove(instance, out ProjectileId id);
            _idToProjectiles.Remove(id);
            return id;
        }

        public bool TryInitializePool(IProjectileConfig config)
        {
            if (_pools.ContainsKey(config)) return false;

            int initialSize = 25;
            int maxSize = 1000;

            ObjectPool<ProjectileBehaviour> pool = new ObjectPool<ProjectileBehaviour>(
                createFunc: () => CreateProjectile(config),
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: DestroyProjectile,
                collectionCheck: true,
                initialSize,
                maxSize);

            // Prevent stutters during gameplay
            var warmup = ArrayPool<ProjectileBehaviour>.Shared.Rent(initialSize);

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

        private void DestroyProjectile(ProjectileBehaviour instance)
        {
            if (instance) Object.Destroy(instance.gameObject);
        }

        private ProjectileBehaviour CreateProjectile(IProjectileConfig config)
        {
            ProjectileBehaviour instance = config.Create() as ProjectileBehaviour;
            _container.InjectGameObject(instance.gameObject);
            return instance;
        }
    }
}
