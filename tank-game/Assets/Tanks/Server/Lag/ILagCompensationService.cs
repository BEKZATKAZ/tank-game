using System;
using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Server.Lag
{
    public struct ProjectileData
    {
        public IProjectileBehaviour Behaviour;
        public IWeaponHolderBehaviour Owner;
    }

    public interface ILagCompensationService
    {
        EntitySnapshot GetEntitySnapshot(EntityId id, int tick);
        ProjectileSnapshot GetProjectileSnapshot(ProjectileId id, int tick);
        ProjectileData GetProjectileData(ProjectileId id);

        bool IsHitAtTick(
            EntityId entityId, ProjectileId projectileId, int tick, out int usedTick);

        void CaptureEntity(EntityId id, int tick, in EntitySnapshot snapshot);

        void CaptureProjectile(
            ProjectileId id, ProjectileData data, int tick, in ProjectileSnapshot snapshot);
    }

    public sealed class LagCompensationService :
        ILagCompensationService,
        IMessageHandler<GameStartedFromServer>,
        IDisposable
    {
        public LagCompensationService(IHitSimulator simulator, IMessageBus bus)
        {
            _simulator = simulator;
            _bus = bus;

            _entitySnapshots = new(initialPoolSize: 8, BUFFER_SIZE);
            _projectileSnapshots = new(initialPoolSize: 64, BUFFER_SIZE);

            _bus.RegisterHandler<GameStartedFromServer>(this);
        }

        public const int BUFFER_SIZE = 64;

        private readonly SnapshotBuffer<EntityId, EntitySnapshot> _entitySnapshots;
        private readonly SnapshotBuffer<ProjectileId, ProjectileSnapshot> _projectileSnapshots;
        private readonly Dictionary<ProjectileId, ProjectileData> _projectileData = new();
        private readonly IHitSimulator _simulator;
        private readonly IMessageBus _bus;
        private int _latestTick;

        public void Dispose()
        {
            _bus.UnregisterHandler<GameStartedFromServer>(this);
            _entitySnapshots.Dispose();
            _projectileSnapshots.Dispose();
        }

        public EntitySnapshot GetEntitySnapshot(EntityId id, int tick)
        {
            return _entitySnapshots.TryGet(id, tick, out var result) ? result : default;
        }

        public ProjectileSnapshot GetProjectileSnapshot(ProjectileId id, int tick)
        {
            return _projectileSnapshots.TryGet(id, tick, out var result) ? result : default;
        }

        public ProjectileData GetProjectileData(ProjectileId id)
        {
            return _projectileData[id];
        }

        public bool IsHitAtTick(
            EntityId targetId, ProjectileId projectileId, int tick, out int usedTick)
        {
            // Prevent ticks from the future and from the far past
            int minTick = math.max(_latestTick - BUFFER_SIZE, 0);
            usedTick = math.clamp(tick, minTick, _latestTick);

            UnityEngine.Debug.LogFormat(
                "Requested lag compensation at client tick {0}, using tick {1}",
                tick, usedTick);

            if (_entitySnapshots.TryGet(targetId, usedTick, out var entity) == false)
            {
                UnityEngine.Debug.LogWarning("Invalid entity requested at lag compensation");
                return false;
            }

            if (_projectileSnapshots.TryGet(projectileId, usedTick, out var projectile) == false)
            {
                UnityEngine.Debug.LogWarning("Invalid projectile requested at lag compensation");
                return false;
            }

            return _simulator.Overlaps(entity, projectile);
        }

        public void CaptureEntity(EntityId id, int tick, in EntitySnapshot snapshot)
        {
            _entitySnapshots.Add(id, tick, in snapshot);
            _latestTick = math.max(_latestTick, tick);
        }

        public void CaptureProjectile(
            ProjectileId id, ProjectileData data, int tick, in ProjectileSnapshot snapshot)
        {
            _projectileSnapshots.Add(id, tick, in snapshot);
            _projectileData[id] = data;
            _latestTick = math.max(_latestTick, tick);
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            _entitySnapshots.Clear();
            _projectileSnapshots.Clear();
            _projectileData.Clear();
        }
    }
}
