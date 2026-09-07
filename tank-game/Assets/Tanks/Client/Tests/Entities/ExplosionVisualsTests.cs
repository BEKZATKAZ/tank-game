using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Client.Entities.Health;
using Tanks.Client.Player;
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

            _component = new ExplosionVisuals(_visuals, _spectatorService);
            _component.Initialize(Substitute.For<IHealthBehaviour>());
        }

        [Test]
        public async Task Explode_SpectatesOther()
        {
            ParticleSystem particleSystem = new GameObject().AddComponent<ParticleSystem>();

            try
            {
                IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
                from.IsValid.Returns(true);
                float2 position = new float2(10, 20);

                _visuals
                    .Play<ParticleSystem>(VisualType.Explosion_1, position)
                    .Returns(particleSystem);

                _component.Handle(new Died { From = from, WorldPosition = position });
                await Task.Delay(2000);

                _visuals.Received(1).Play<ParticleSystem>(VisualType.Explosion_1, position);
                _spectatorService.Received(1).Spectate(from.AsRequired());
            }
            finally
            {
                Object.DestroyImmediate(particleSystem.gameObject);
            }
        }
    }
}
