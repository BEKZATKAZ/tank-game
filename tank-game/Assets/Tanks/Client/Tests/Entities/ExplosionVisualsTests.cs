using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Tests.Entities
{
    public class ExplosionVisualsTests
    {
        private IVisualService _visuals;
        private ISpectatorService _spectatorService;
        private ExplosionVisuals _component;

        [SetUp]
        public void Setup()
        {
            _visuals = Substitute.For<IVisualService>();
            _spectatorService = Substitute.For<ISpectatorService>();

            _component = new ExplosionVisuals(
                _visuals, _spectatorService,
                Substitute.For<ICameraShakeService>(), Substitute.For<IMessageBus>());

            _component.Initialize(Substitute.For<IHealthBehaviour>());
        }

        [Test]
        public void Explode_NextRoundStarted_CancelsSpectating()
        {
            ParticleSystem particleSystem = new GameObject().AddComponent<ParticleSystem>();

            try
            {
                IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
                float2 position = new float2(10, 20);

                from.IsValid.Returns(true);
                from.IsPossessed.Returns(true);

                _visuals
                    .Play<ParticleSystem>(VisualType.Explosion_1, position)
                    .Returns(particleSystem);

                _component.Handle(new Died
                {
                    Target = from,
                    From = from,
                    WorldPosition = position
                });

                _component.Update(deltaTime: 1.9f);
                _component.Handle(new GameStartedFromServer());

                _component.Update(deltaTime: 0.12f);
                _spectatorService.DidNotReceiveWithAnyArgs().Spectate(default);
            }
            finally
            {
                Object.DestroyImmediate(particleSystem.gameObject);
            }
        }

        [Test]
        public void Explode_LocalPlayer_SpectatesOther()
        {
            ParticleSystem particleSystem = new GameObject().AddComponent<ParticleSystem>();

            try
            {
                IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
                float2 position = new float2(10, 20);

                from.IsValid.Returns(true);
                from.IsPossessed.Returns(true);

                _visuals
                    .Play<ParticleSystem>(VisualType.Explosion_1, position)
                    .Returns(particleSystem);

                _component.Update(deltaTime: 0.1f);

                _component.Handle(new Died
                {
                    Target = from,
                    From = from,
                    WorldPosition = position
                });

                _component.Update(deltaTime: 2.5f);

                _spectatorService.Received(1).Spectate(from.AsRequired());
                _visuals.Received(1).Play<ParticleSystem>(VisualType.Explosion_1, position);
            }
            finally
            {
                Object.DestroyImmediate(particleSystem.gameObject);
            }
        }

        [Test]
        public void Explode_OtherPlayer_DoesNothing()
        {
            ParticleSystem particleSystem = new GameObject().AddComponent<ParticleSystem>();

            try
            {
                IEntityBehaviour localPlayer = Substitute.For<IEntityBehaviour>();
                IEntityBehaviour otherPlayer = Substitute.For<IEntityBehaviour>();
                float2 position = new float2(10, 20);

                localPlayer.IsValid.Returns(true);
                localPlayer.IsPossessed.Returns(true);

                otherPlayer.IsValid.Returns(true);
                otherPlayer.IsPossessed.Returns(false);

                _visuals
                    .Play<ParticleSystem>(VisualType.Explosion_1, position)
                    .Returns(particleSystem);

                _component.Handle(new Died
                {
                    Target = otherPlayer,
                    From = otherPlayer,
                    WorldPosition = position
                });

                _component.Update(deltaTime: 5);

                _spectatorService
                    .DidNotReceiveWithAnyArgs()
                    .Spectate(localPlayer.AsRequired());
            }
            finally
            {
                Object.DestroyImmediate(particleSystem.gameObject);
            }
        }
    }
}
