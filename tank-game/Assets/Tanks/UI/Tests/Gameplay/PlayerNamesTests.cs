using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Teams;
using Tanks.UI.Gameplay.PlayerNames;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public class PlayerNamesTests : ViewModelTestBase<PlayerNamesViewModel>
    {
        private List<IEntityBehaviour> _entities;
        private IClientEntityService _clientEntity;
        private ICameraMainService _cameraService;
        private ICamera _camera;
        private SpriteRenderer _spriteRenderer;

        protected override PlayerNamesViewModel CreateViewModel()
        {
            _spriteRenderer = new GameObject("Player Sprite", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();

            _entities = new List<IEntityBehaviour>();
            IEntityService entities = Substitute.For<IEntityService>();
            entities.GetAllEntities().Returns(_entities);
            _clientEntity = Substitute.For<IClientEntityService>();

            _camera = Substitute.For<ICamera>();
            _camera.ViewportRect.Returns(new Rect(0, 0, 1, 1));
            _camera.WorldToRenderedViewportPoint(Arg.Any<float2>()).Returns(x => (float2)x[0]);
            _cameraService = Substitute.For<ICameraMainService>();
            _cameraService.Get().Returns(_camera);

            return new PlayerNamesViewModel(entities, _clientEntity, _cameraService);
        }

        [TearDown]
        public void DestroySpriteRenderer()
        {
            Object.DestroyImmediate(_spriteRenderer.gameObject);
        }

        [Test]
        public void Tick_ProjectsNameAboveTankAndFlipsVerticalAxis()
        {
            IEntityBehaviour entity = CreatePlayer("Player", new float2(0.25f, 0.4f));
            entity.Size.Returns(new float2(0.2f, 0.2f));
            ViewModel.Tick();

            PlayerNameEntry entry = ViewModel.Entries[entity];
            Assert.IsTrue(entry.IsVisible);
            Assert.AreEqual("Player", entry.DisplayName);
            Assert.AreEqual(new Vector2(0.25f, 0.5f), entry.ScreenPosition);
        }

        [Test]
        public void Tick_PossessionChangeExcludesNewTargetAndRestoresPreviousName()
        {
            IEntityBehaviour first = CreatePlayer("First", new float2(0.2f));
            IEntityBehaviour second = CreatePlayer("Second", new float2(0.4f));
            _clientEntity.GetPossessedEntity().Returns(first);
            ViewModel.Tick();

            Assert.IsFalse(ViewModel.Entries.ContainsKey(first));
            Assert.IsTrue(ViewModel.Entries[second].IsVisible);

            _clientEntity.GetPossessedEntity().Returns(second);
            ViewModel.Tick();

            Assert.IsTrue(ViewModel.Entries[first].IsVisible);
            Assert.IsFalse(ViewModel.Entries.ContainsKey(second));
        }

        [TestCase(-0.1f, 0.5f)]
        [TestCase(1.1f, 0.5f)]
        [TestCase(0.5f, -0.1f)]
        [TestCase(0.5f, 1.1f)]
        public void Tick_OffscreenFreezesTextAndPositionUntilReentry(float x, float y)
        {
            IEntityBehaviour entity = CreatePlayer("Before", new float2(0.5f));
            ViewModel.Tick();
            Vector2 previous = ViewModel.Entries[entity].ScreenPosition;

            entity.Transform.WorldPosition.Returns(new float2(x, y));
            entity.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(new PlayerState(1, "After", default));
            ViewModel.Tick();

            Assert.IsFalse(ViewModel.Entries[entity].IsVisible);
            Assert.AreEqual("Before", ViewModel.Entries[entity].DisplayName);
            Assert.AreEqual(previous, ViewModel.Entries[entity].ScreenPosition);

            entity.Transform.WorldPosition.Returns(new float2(0.25f));
            ViewModel.Tick();

            Assert.IsTrue(ViewModel.Entries[entity].IsVisible);
            Assert.AreEqual("After", ViewModel.Entries[entity].DisplayName);
            Assert.AreEqual(new Vector2(0.25f, 0.75f), ViewModel.Entries[entity].ScreenPosition);
        }

        [Test]
        public void Tick_SpectatorNameFollowsPossessionAndClearsForLocalPlayer()
        {
            IEntityBehaviour local = CreatePlayer("Local", new float2(0.2f));
            IEntityBehaviour first = CreatePlayer("First", new float2(0.4f));
            IEntityBehaviour second = CreatePlayer("Second", new float2(0.6f));
            local.IsLocalPlayer.Returns(true);

            _clientEntity.GetPossessedEntity().Returns(first);
            ViewModel.Tick();
            Assert.AreEqual("First", ViewModel.SpectatorName);

            _clientEntity.GetPossessedEntity().Returns(second);
            ViewModel.Tick();
            Assert.AreEqual("Second", ViewModel.SpectatorName);

            _clientEntity.GetPossessedEntity().Returns(local);
            ViewModel.Tick();
            Assert.IsEmpty(ViewModel.SpectatorName);

            _clientEntity.GetPossessedEntity().Returns((IEntityBehaviour)null);
            ViewModel.Tick();
            Assert.IsEmpty(ViewModel.SpectatorName);
        }

        [Test]
        public void Tick_RemovesDeadDespawnedInvalidAndEliminatedPlayers()
        {
            IEntityBehaviour dead = CreatePlayer("Dead", new float2(0.5f));
            IEntityBehaviour despawned = CreatePlayer("Despawned", new float2(0.5f));
            IEntityBehaviour invalid = CreatePlayer("Invalid", new float2(0.5f));
            IEntityBehaviour eliminated = CreatePlayer("Eliminated", new float2(0.5f));
            ViewModel.Tick();
            Assert.AreEqual(4, ViewModel.Entries.Count);

            dead.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(0, 100));
            _entities.Remove(despawned);
            invalid.IsValid.Returns(false);
            eliminated.GetBehaviour<ITeamParticipantBehaviour>().State.SetEliminated(true);
            ViewModel.Tick();

            Assert.IsEmpty(ViewModel.Entries);
        }

        [Test]
        public void Tick_MissingCameraHidesNamesAndRecovers()
        {
            IEntityBehaviour entity = CreatePlayer("Player", new float2(0.5f));
            ViewModel.Tick();

            _cameraService.Get().Returns((ICamera)null);
            ViewModel.Tick();
            Assert.IsFalse(ViewModel.Entries[entity].IsVisible);

            _cameraService.Get().Returns(_camera);
            ViewModel.Tick();
            Assert.IsTrue(ViewModel.Entries[entity].IsVisible);
        }

        [Test]
        public void Tick_AccountsForCameraViewportWithinScreen()
        {
            IEntityBehaviour entity = CreatePlayer("Player", new float2(0.25f));
            _camera.ViewportRect.Returns(new Rect(0.1f, 0.2f, 0.8f, 0.6f));
            ViewModel.Tick();

            Vector2 position = ViewModel.Entries[entity].ScreenPosition;
            Assert.That(position.x, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(0.65f).Within(0.0001f));
        }

        [Test]
        public void Tick_HidesNameWhenAnchorIsAboveScreen()
        {
            IEntityBehaviour entity = CreatePlayer("Player", new float2(0.5f, 0.95f));
            entity.Size.Returns(new float2(0.2f, 0.2f));
            ViewModel.Tick();

            Assert.IsFalse(ViewModel.Entries[entity].IsVisible);
            Assert.IsNull(ViewModel.Entries[entity].DisplayName);
        }

        private IEntityBehaviour CreatePlayer(string name, float2 position)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            entity.IsValid.Returns(true);
            entity.Transform.Returns(Substitute.For<ITransform>());
            entity.Transform.WorldPosition.Returns(position);
            entity.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(100, 100));
            entity.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(new PlayerState(1, name, default));

            IEntityRenderer renderer = Substitute.For<IEntityRenderer>();
            renderer.GetAllSpriteRenderers().Returns(new[] { _spriteRenderer });
            entity.Require<IEntityRenderer>().Returns(renderer);
            _entities.Add(entity);

            return entity;
        }
    }
}
