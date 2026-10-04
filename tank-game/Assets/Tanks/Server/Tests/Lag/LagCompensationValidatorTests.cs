using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Server.Lag;
using Tanks.Shared.Entities;
using Tanks.Shared.Lag;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;

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

            ProjectileData data = new ProjectileData
            {
                Behaviour = Substitute.For<IProjectileBehaviour>(),
                Owner = Substitute.For<IWeaponHolderBehaviour>()
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

            _entityService.GetById(message.TargetId).Returns(target);
            target.IsValid.Returns(true);

            _component.Handle(message);

            var modules = data.Behaviour.GetBehaviour<IProjectileModuleList>();
            modules.Received(1).Call(Arg.Any<Action<ProjectileModuleBehaviourBase>>());

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
