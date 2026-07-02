using NaughtyAttributes;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Tanks.Shared.Weapons.Projectiles
{
    public sealed class UnityProjectilePoolService : MonoBehaviour, IProjectilePoolService
    {
        [Required]
        [SerializeField]
        private ProjectileComponent _prefab;

        [Range(10, 100)]
        [SerializeField]
        private int _initialPoolSize = 10;

        [Range(200, 10000)]
        [SerializeField]
        private int _maxPoolSize = 1000;

        private ObjectPool<ProjectileComponent> _pool;
        private Transform _transform;

        public void LaunchAt(
            float2 worldPosition, Angle worldRotation, LaunchSettings settings)
        {
            ProjectileComponent instance = _pool.Get();
            instance.Transform.WorldPosition = worldPosition;
            instance.Transform.WorldRotation = worldRotation;
            instance.Launch(settings);
        }

        public void Release(IProjectile instance)
        {
            _pool.Release(instance as ProjectileComponent);
        }

        private void Awake()
        {
            _transform = transform;
            _pool = new ObjectPool<ProjectileComponent>(
                createFunc: () => Instantiate(_prefab, _transform),
                actionOnGet: x => x.gameObject.SetActive(true),
                actionOnRelease: x => x.gameObject.SetActive(false),
                actionOnDestroy: x => Destroy(x),
                collectionCheck: false,
                defaultCapacity: _initialPoolSize,
                maxSize: _maxPoolSize
            );
        }

        private void OnDestroy()
        {
            _pool.Dispose();
        }
    }
}