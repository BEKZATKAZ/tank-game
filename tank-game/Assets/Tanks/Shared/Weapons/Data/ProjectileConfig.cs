using NaughtyAttributes;
using Tanks.Core.Helpers;
using Tanks.Shared.Health.Data;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Shared.Weapons.Data
{
    public interface IProjectileConfig
    {
        float Speed { get; }
        float LifeTime { get; }
        ushort MaxRicochetCount { get; }

        IProjectile Create();
    }

    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptable Objects/Weapons/Projectile")]
    public class ProjectileConfig : ScriptableObject, IProjectileConfig
    {
        [Required]
        [SerializeField]
        private ProjectileBehaviour _prefab;

        [Required]
        [SerializeField]
        private DamageConfig _damage;

        [Range(5, 50)]
        [SerializeField]
        private float _speed = 15;

        [Range(1, 20)]
        [SerializeField]
        private float _lifeTime = 10;

        [Range(0, 20)]
        [SerializeField]
        private ushort _maxRicochetCount = 10;

        public float Speed => _speed;
        public float LifeTime => _lifeTime;
        public ushort MaxRicochetCount => _maxRicochetCount;

        public IProjectile Create()
        {
            ProjectileBehaviour instance = Instantiate(_prefab);
            instance.Initialize(this, _damage.Create().AsRequired());
            return instance;
        }
    }
}
