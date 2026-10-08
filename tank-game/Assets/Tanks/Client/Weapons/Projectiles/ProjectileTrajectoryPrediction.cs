using System.Collections.Generic;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Weapons.Projectiles
{
    public sealed class ProjectileTrajectoryPrediction : MonoBehaviour
    {
        private readonly List<LineRenderer> _lines = new();
        private WeaponHolderBehaviour _owner;
        private Vector3[] _positions;

        private Vector2 CalculateDirection(int iteration, int count, float spread)
        {
            float t = (iteration + 0.5f) / count;
            float offset = math.lerp(-spread, spread, t);

            Angle rotation = _owner.MuzzlePoint.WorldRotation + Angle.FromDegrees(offset);
            float radians = rotation.Radians;

            return new Vector2(math.cos(radians), math.sin(radians));
        }

        private void Start()
        {
            _owner = GetComponentInParent<WeaponHolderBehaviour>();
            _lines.AddRange(GetComponentsInChildren<LineRenderer>());
            _positions = new Vector3[2];
        }

        private void LateUpdate()
        {
            if (_owner.Entity.IsPossessed == false ||
                _owner.CurrentWeapon is null ||
                _owner.CurrentWeapon.UseAction is not ProjectileLaunchAction action)
            {
                foreach (LineRenderer line in _lines) line.enabled = false;
                return;
            }

            float radius = action.Config.Prefab.Radius;
            float distance = action.Config.Prefab.LifeTime * action.Config.Prefab.Speed;
            float spread = action.Config.SpreadDegrees;

            Vector2 start = _owner.MuzzlePoint.WorldPosition;
            int projectileCount = action.Config.ProjectileCount;

            for (int i = 0; i < projectileCount - _lines.Count; i++)
            {
                LineRenderer newLine = Instantiate(_lines[0], transform.parent);
                _lines.Add(newLine);
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                LineRenderer line = _lines[i];

                if (i >= projectileCount)
                {
                    line.enabled = false;
                    continue;
                }

                Vector2 direction = CalculateDirection(i, projectileCount, spread);
                RaycastHit2D hit = Physics2D.CircleCast(start, radius, direction, distance);

                if (hit) distance = hit.distance;

                _positions[0] = start;
                _positions[1] = start + direction * distance;

                line.enabled = true;
                line.startWidth = radius;
                line.SetPositions(_positions);
            }
        }
    }
}
