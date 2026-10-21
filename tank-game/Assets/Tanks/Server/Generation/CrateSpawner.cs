using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Tanks.Server.Generation.Crates
{
    public sealed class CrateSpawner : MonoBehaviour, ICrateSpawner
    {
        [SerializeField] private Dictionary<ItemTier, CrateBehaviour> _cratePrefabs = new();
        [SerializeField] private CrateGenerationSettings _settings;

        private readonly Dictionary<ItemTier, ObjectPool<ICrateBehaviour>> _pools = new();
        private readonly Dictionary<ItemTier, List<ICrateBehaviour>> _activeCrates = new();
        private ICrateGenerator _generator;
        private IObjectResolver _container;
        private Transform _transform;

        public IDictionary<ItemTier, CrateBehaviour> CratePrefabs => _cratePrefabs;

        [Inject]
        public void Construct(ICrateGenerator generator, IObjectResolver container)
        {
            _generator = generator;
            _container = container;
        }

        public void Return(ICrateBehaviour instance)
        {
            ItemTier tier = instance.Chances.First().Key;
            ResetHealth(instance);

            _activeCrates[tier].Remove(instance);
            _pools[tier].Release(instance);
        }

        public ICrateBehaviour Spawn(GeneratedCrate crate)
        {
            ICrateBehaviour instance = _pools[crate.Tier].Get();

            // Add 1 (x:1, y:1) to count from bottom left corner
            instance.Transform.WorldPosition = crate.Position + 1;

            if (_activeCrates.TryGetValue(crate.Tier, out List<ICrateBehaviour> list) == false)
            {
                list = new List<ICrateBehaviour>();
                _activeCrates.Add(crate.Tier, list);
            }

            list.Add(instance);

            return instance;
        }

        public void Spawn(ReadOnlySpan<GeneratedTile> tiles, uint seed)
        {
            using var crates = _generator.Generate(tiles, seed, new CrateGenerationSettings
            {
                ClusterDensity = 8,
                ClusterRadius = 8,
                MinClusterDistance = 30,
                MinMaxCratesPerCluster = new int2(4, 16),
                CornerBias = 6,

                EpicCrateCount = 3,
                EpicRadius = 2,

                RareCrateDensity = 10,
                MinSingleCrateDistance = 30
            });

            foreach (GeneratedCrate crate in crates.Value) Spawn(crate);
        }

        public void Clear()
        {
            foreach ((ItemTier tier, List<ICrateBehaviour> list) in _activeCrates)
            {
                foreach (ICrateBehaviour instance in list)
                {
                    ResetHealth(instance);

                    if (_pools.TryGetValue(tier, out var pool) == false) continue;
                    pool.Release(instance);
                }

                list.Clear();
            }
        }

        private void ResetHealth(ICrateBehaviour instance)
        {
            IHealthBehaviour healthBehaviour = instance.GetBehaviour<IHealthBehaviour>();
            IAuthoritativeHealth health = healthBehaviour.Require<IAuthoritativeHealth>();

            if (health is null) return;

            float initialHealth = healthBehaviour.InitialHealth;
            health.SetState(new HealthState(initialHealth, initialHealth));
        }

        private CrateBehaviour CreateInstance(CrateBehaviour prefab)
        {
            CrateBehaviour instance = Instantiate(prefab, _transform);
            _container.InjectGameObject(instance.gameObject);

            instance.NetworkObject.Spawn(destroyWithScene: true);
            return instance;
        }

        private void DestroyInstance(ICrateBehaviour instance)
        {
            if (instance is not CrateBehaviour behaviour) return;
            if (behaviour) Destroy(behaviour.gameObject);
        }

        private void Awake()
        {
            _transform = transform;

            if (_cratePrefabs != null)
            {
                foreach ((ItemTier tier, CrateBehaviour prefab) in _cratePrefabs)
                {
                    _pools[tier] = new ObjectPool<ICrateBehaviour>(
                        createFunc: () => CreateInstance(prefab),
                        actionOnRelease: x => x.Transform.WorldPosition = -999,
                        actionOnDestroy: DestroyInstance,
                        defaultCapacity: 32);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (ObjectPool<ICrateBehaviour> pool in _pools.Values) pool.Dispose();
        }
    }
}
