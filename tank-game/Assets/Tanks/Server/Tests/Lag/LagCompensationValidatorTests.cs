using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Server.Lag;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Lag;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Tests.Lag
{
    public class LagCompensationValidatorTests
    {
        private ILagCompensationService _service;
        private IProjectilePoolService _projectilePool;
        private IEntityService _entityService;
        private LagCompensationRequestValidator _component;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<ILagCompensationService>();
            _projectilePool = Substitute.For<IProjectilePoolService>();
            _entityService = Substitute.For<IEntityService>();

            _component = new LagCompensationRequestValidator(
                _service, _projectilePool, _entityService);

            _component.Initialize(Substitute.For<ILagCompensationGateway>());
        }

        [Test]
        public void Handle_DoesNothing_WhenHitIsInvalid()
        {
            HitRequest message = CreateMessage();

            _service
                .IsHitAtTick(message.TargetId, message.ProjectileId, message.ClientTick, out _)
                .Returns(false);

            _component.Handle(message);

            _entityService.DidNotReceive().GetById(message.TargetId);
            _projectilePool.DidNotReceive().ReturnProjectileClientRpc(message.ProjectileId);
            _projectilePool.DidNotReceive().Return(message.ProjectileId);
        }

        [Test]
        public void Handle_DoesNothing_WhenTargetIsInvalid()
        {
            HitRequest message = CreateMessage();
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();

            _service
                .IsHitAtTick(message.TargetId, message.ProjectileId, message.ClientTick, out _)
                .Returns(false);

            _entityService.GetById(message.TargetId).Returns(target);
            target.IsValid.Returns(false);

            _component.Handle(message);

            _service.DidNotReceive().GetProjectileData(message.ProjectileId);
            _projectilePool.DidNotReceive().ReturnProjectileClientRpc(message.ProjectileId);
            _projectilePool.DidNotReceive().Return(message.ProjectileId);
        }

        [Test]
        public void Handle_DamagesTarget_ReturnsProjectile()
        {
            HitRequest message = CreateMessage();

            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IHealthBehaviour health = Substitute.For<IHealthBehaviour>();
            IAuthoritativeHealth authoritativeHealth = Substitute.For<IAuthoritativeHealth>();
            IWeaponHolderBehaviour owner = Substitute.For<IWeaponHolderBehaviour>();
            IStatOwnerBehaviour statOwner = Substitute.For<IStatOwnerBehaviour>();
            IDamage damage = Substitute.For<IDamage>();

            ProjectileData data = new ProjectileData
            {
                Damage = damage,
                Owner = owner
            };

            ProjectileSnapshot snapshot = new ProjectileSnapshot(default, 0.5f, 2);

            _service
                .IsHitAtTick(
                    message.TargetId, message.ProjectileId, message.ClientTick,
                    out Arg.Any<int>())
                .Returns(args =>
                {
                    args[3] = message.ClientTick; // Used tick
                    return true;
                });

            _service.GetProjectileData(message.ProjectileId).Returns(data);
            _service.GetProjectileSnapshot(message.ProjectileId, 10).Returns(snapshot);

            target.GetBehaviour<IHealthBehaviour>().Returns(health);
            health.Require<IAuthoritativeHealth>().Returns(authoritativeHealth);

            owner.GetBehaviour<IStatOwnerBehaviour>().Returns(statOwner);
            damage.Evaluate(2).Returns(50);
            statOwner.GetStat(StatType.Damage, 50).Returns(75);

            _entityService.GetById(message.TargetId).Returns(target);
            target.IsValid.Returns(true);

            _component.Handle(message);

            HealthEventId eventId = HealthEventId
                .Create(typeof(IProjectileBehaviour), message.ProjectileId);

            authoritativeHealth.Received(1).Damage(75, owner.Entity, eventId);

            _projectilePool.Received(1).ReturnProjectileClientRpc(message.ProjectileId);
            _projectilePool.Received(1).Return(message.ProjectileId);
        }

        private static HitRequest CreateMessage(
            EntityId targetId = default, ProjectileId projectileId = default)
        {
            return new HitRequest
            {
                TargetId = targetId,
                ProjectileId = projectileId,
                ClientTick = 10
            };
        }
    }
}
