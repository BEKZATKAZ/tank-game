using NaughtyAttributes;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

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

        [Inject]
        private readonly IObjectResolver _resolver;

        private ObjectPool<ProjectileComponent> _pool;
        private Transform _transform;

        public void LaunchAt(
            float2 worldPosition, Angle worldRotation, LaunchSettings settings)
        {
            ProjectileComponent instance = _pool.Get();

            instance.Transform.WorldPosition = worldPosition;
            instance.Transform.WorldRotation = worldRotation;

            instance.Launch(settings, new LaunchCallbacks { OnDone = Release });
        }

        public void Release(IProjectile instance)
        {
            _pool.Release(instance as ProjectileComponent);
        }

        private void Start()
        {
            _transform = transform;
            _pool = new ObjectPool<ProjectileComponent>(
                createFunc: () =>
                {
                    var instance = Instantiate(_prefab, _transform);
                    _resolver.InjectGameObject(instance.gameObject);
                    return instance;
                },
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