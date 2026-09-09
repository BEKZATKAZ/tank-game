using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Client.Entities.Health;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
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
                _visuals, _spectatorService, Substitute.For<ICameraShakeService>());

            _component.Initialize(Substitute.For<IHealthBehaviour>());
        }

        [Test]
        public async Task Explode_LocalPlayer_SpectatesOther()
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

                await Task.Delay(2000);

                _visuals.Received(1).Play<ParticleSystem>(VisualType.Explosion_1, position);
                _spectatorService.Received(1).Spectate(from.AsRequired());
            }
            finally
            {
                Object.DestroyImmediate(particleSystem.gameObject);
            }
        }

        [Test]
        public async Task Explode_OtherPlayer_DoesNothing()
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

                await Task.Delay(2000);

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
