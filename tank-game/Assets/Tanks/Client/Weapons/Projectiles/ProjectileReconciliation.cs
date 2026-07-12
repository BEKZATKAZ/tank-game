using System;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.Client.Weapons
{
    public sealed class ProjectileReconciliation : IDisposable
    {
        public ProjectileReconciliation(
            IProjectile projectile,
            float positionThreshold = 0.001f,
            float degreeThreshold = 10)
        {
            _tickSnapshots = new RingBuffer<ProjectileSnapshot>(128);
            _projectile = projectile;
            _positionThreshold = positionThreshold;
            _degreeThreshold = degreeThreshold;
        }

        private readonly RingBuffer<ProjectileSnapshot> _tickSnapshots;
        private readonly IProjectile _projectile;
        private readonly float _positionThreshold;
        private readonly float _degreeThreshold;

        private int _lastServerTick;
        private int _currentTick;

        private ProjectileSnapshot CaptureSnapshot(ITransform transform)
        {
            return new ProjectileSnapshot
            {
                Position = transform.WorldPosition,
                Rotation = transform.WorldRotation
            };
        }

        public void Reset(int tick, ITransform predictedTransform)
        {
            _tickSnapshots[tick] = CaptureSnapshot(predictedTransform);
        }

        public bool TryReconcile(
            ITransform predictedTransform,
            ProjectileSnapshotFromServer snapshot,
            float tickDelta,
            out float2 positionError)
        {
            positionError = default;

            if (_lastServerTick >= snapshot.ServerTick) return false;

            _lastServerTick = snapshot.ServerTick;
            ref ProjectileSnapshot start = ref _tickSnapshots[snapshot.ServerTick];

            float posDiff = math.distance(start.Position, snapshot.ServerPosition);
            float angDiff = math.abs(start.Rotation.Degrees - snapshot.ServerRotation.Degrees);

            if (posDiff < _positionThreshold && angDiff < _degreeThreshold) return false;

            predictedTransform.WorldPosition = snapshot.ServerPosition;
            predictedTransform.WorldRotation = snapshot.ServerRotation;

            // Replay simulation
            for (int i = snapshot.ServerTick; i < _currentTick; i++)
            {
                _projectile.Simulate(default, predictedTransform, tickDelta);
            }

            float2 predictedPosition = predictedTransform.WorldPosition;
            positionError = _projectile.Transform.WorldPosition - predictedPosition;

            // Snap instantly if too far
            if (math.length(positionError) > 3) positionError = float2.zero;

            return true;
        }

        public void Tick(int tick, ITransform predictedTransform)
        {
            _tickSnapshots[tick] = CaptureSnapshot(predictedTransform);
            _currentTick = tick;
        }

        public void Dispose()
        {
            _tickSnapshots.Dispose();
        }
    }
}
