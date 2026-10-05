using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using VContainer;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public sealed class DirectDamageModule : DamageModuleBase
    {
        public ushort NearDamage = 20;
        public ushort FarDamage = 10;

        public byte MinTime = 1;
        public byte MaxTime = 10;

        public override PositiveFloat MaxDamage => math.max((float)NearDamage, FarDamage);

        private IDamageService _service;

        [Inject]
        public void Construct(IDamageService service)
        {
            _service = service;
        }

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            float t = math.unlerp(MinTime, MaxTime, Projectile.ElapsedSeconds);
            float damage = math.lerp(NearDamage, FarDamage, math.saturate(t));

            HealthEventId eventId = HealthEventId.Create(GetType(), Id);
            _service.Damage(damage, target, from, eventId, includeStats: true);
        }
    }
}
