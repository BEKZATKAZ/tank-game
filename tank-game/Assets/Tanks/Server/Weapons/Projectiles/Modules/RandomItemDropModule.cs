using System.Collections.Generic;
using Tanks.Server.Items.Pickups;
using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Weapons.Projectiles.Modules
{
    public sealed class RandomItemDropModule : ProjectileModuleBehaviourBase,
        INetworkMessageHandler<ProjectileLaunched>
    {
        public float WallClearance = 0.5f;
        public LayerMask ObstacleMask = 1;

        private const float SearchStep = 0.1f;
        private const int SearchRings = 20;
        private const int SearchDirections = 16;

        private readonly System.Random _random = new(
            System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue));
        private readonly List<Collider2D> _overlaps = new();
        private readonly List<RaycastHit2D> _hits = new();

        private IItemDatabase _items;
        private IPickupSpawnBehaviour _pickups;
        private bool _spawned;
        private float2 _launchPosition;

        [Inject]
        public void Construct(IItemDatabase items, IPickupSpawnBehaviour pickups)
        {
            _items = items;
            _pickups = pickups;
            Projectile.RegisterMessageHandler(this);
        }

        public void Handle(ProjectileLaunched message)
        {
            _spawned = false;
            _launchPosition = Projectile.Transform.WorldPosition;
        }

        protected override void OnProjectileDone()
        {
            if (Projectile.Owner.IsServer == false || _spawned) return;
            _spawned = true;

            IItemConfig item = ItemDropChooser.Choose(_items.Items, _random);
            if (item == null) return;

            if (TryFindPosition(out float2 point) == false) return;

            _pickups.SpawnEveryoneRpc(new PickupSpawned
            {
                ItemId = item.Identifier,
                WorldPosition = point
            });
        }

        private bool TryFindPosition(out float2 point)
        {
            float2 impact = Projectile.Transform.WorldPosition;
            float2 anchor = impact;
            ContactFilter2D filter = new()
            {
                useLayerMask = true,
                layerMask = ObstacleMask,
                useTriggers = false
            };

            // Recover the firing side if the final simulation step ended inside a wall.
            if (Physics2D.OverlapPoint(impact, filter, _overlaps) > 0)
            {
                float2 travel = impact - _launchPosition;
                float distance = math.length(travel);
                if (distance <= 0.001f || Physics2D.OverlapPoint(_launchPosition, filter, _overlaps) > 0)
                {
                    point = default;
                    return false;
                }

                float2 direction = travel / distance;
                if (Physics2D.Raycast(_launchPosition, direction, filter, _hits, distance) == 0)
                {
                    point = default;
                    return false;
                }

                anchor = (float2)_hits[0].point - direction * 0.01f;
            }

            if (IsValidPosition(impact, anchor, filter))
            {
                point = impact;
                return true;
            }

            for (int ring = 1; ring <= SearchRings; ring++)
            {
                for (int i = 0; i < SearchDirections; i++)
                {
                    float angle = i * (math.PI * 2 / SearchDirections);
                    float2 candidate = impact + new float2(math.cos(angle), math.sin(angle)) * (ring * SearchStep);
                    if (IsValidPosition(candidate, anchor, filter) == false) continue;

                    point = candidate;
                    return true;
                }
            }

            float2 back = _launchPosition - anchor;
            float length = math.length(back);
            float2 backward = math.normalizesafe(back);

            for (int step = 1; step <= (int)math.ceil(length / SearchStep); step++)
            {
                float2 candidate = anchor + backward * math.min(step * SearchStep, length);
                if (IsValidPosition(candidate, anchor, filter) == false) continue;

                point = candidate;
                return true;
            }

            point = default;
            return false;
        }

        private bool IsValidPosition(float2 point, float2 anchor, ContactFilter2D filter)
        {
            if (Physics2D.OverlapCircle(point, math.max(0.01f, WallClearance), filter, _overlaps) > 0) return false;

            float2 delta = point - anchor;
            float distance = math.length(delta);
            return distance <= 0.001f || Physics2D.Raycast(anchor, delta / distance, filter, _hits, distance) == 0;
        }
    }
}
