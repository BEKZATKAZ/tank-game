using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Client.Entities.Runtime;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Editor.TestHelpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Client.Tests.Entities
{
    public class EntityVisualsTests
    {
        private GameObject _gameObject;
        private EntityVisuals _visuals;
        private HealthBehaviour _health;
        private EntityBehaviour _entity;
        private EntityVisualConfig _config;
        private IAudioInstance _hitSound;

        private IVisualService _visualService;
        private IAudioPlayer _audioPlayer;
        private IClientEntityService _entityService;
        private ICameraMainService _cameraMain;
        private ICameraShakeService _shakeService;

        private GameObject _particleSystemGo;
        private ParticleSystem _particleSystem;
        private GameObject _damageTextGo;
        private DamagePreviewText _damagePreviewText;

        [SetUp]
        public void Setup()
        {
            _visualService = Substitute.For<IVisualService>();
            _audioPlayer = Substitute.For<IAudioPlayer>();
            _entityService = Substitute.For<IClientEntityService>();
            _cameraMain = Substitute.For<ICameraMainService>();
            _shakeService = Substitute.For<ICameraShakeService>();

            SetupParticleSystem();
            SetupDamagePreviewText();

            _gameObject = new GameObject("TestEntity");
            _visuals = _gameObject.AddComponent<EntityVisuals>();
            _health = _gameObject.GetComponent<HealthBehaviour>();
            _entity = _gameObject.GetComponent<EntityBehaviour>();
            _hitSound = Substitute.For<IAudioInstance>();

            _config = CreateConfig();
            _visuals.SetPrivateField("_config", _config);

            IAudioFactory factory = Substitute.For<IAudioFactory>();
            factory.CreateInstance(_config.HitSoundPath).Returns(_hitSound);

            _visuals.Construct(
                _visualService, _audioPlayer, factory,
                _entityService, _cameraMain, _shakeService);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config) Object.DestroyImmediate(_config);
            if (_gameObject) Object.DestroyImmediate(_gameObject);
            if (_particleSystemGo) Object.DestroyImmediate(_particleSystemGo);
            if (_damageTextGo) Object.DestroyImmediate(_damageTextGo);
        }

        [TestCase(50, 80)]
        [TestCase(50, 50.0001f)]
        [TestCase(50, 50)]
        public void Handle_Prediction_NoDamage_DoesNothing(float oldHealth, float newHealth)
        {
            HealthChangedFromPrediction message = CreatePredictionMessage(oldHealth, newHealth);
            _visuals.Handle(message);
            AssertNothingTriggered();
        }

        [Test]
        public void Handle_Prediction_DamageTaken_PlaysHitSound_WhenConfigured()
        {
            HealthChangedFromPrediction message = CreatePredictionMessage(100, 80);
            _visuals.Handle(message);
            AssertHitSoundPlayed(isPossessed: false);
        }

        [Test]
        public void Handle_Prediction_DamageTaken_PlaysHitVisual_WhenConfigured()
        {
            _config.SetPrivateField("_hitVisualType", VisualType.Explosion_1);
            _config.SetPrivateField("_destroyVisualType", VisualType.Explosion_1);

            HealthChangedFromPrediction message = CreatePredictionMessage(100, 80);
            _visuals.Handle(message);

            AssertHitVisualPlayed();
        }

        [Test]
        public void Handle_Prediction_DamageTaken_SkipsHitVisual_WhenHitVisualTypeNone()
        {
            _config.SetPrivateField("_hitVisualType", VisualType.None);
            HealthChangedFromPrediction message = CreatePredictionMessage(100, 80);

            _visuals.Handle(message);
            AssertHitVisualNotPlayed();
        }

        [Test]
        public void Handle_Prediction_DamageTaken_ShowsDamagePreviewText_WhenNotPossessed()
        {
            _entity.MarkAsUnpossessed();
            HealthChangedFromPrediction message = CreatePredictionMessage(100, 70);

            _visuals.Handle(message);
            AssertDamageTextPlayed();
        }

        [Test]
        public void Handle_Prediction_DamageTaken_SkipsDamagePreviewText_WhenPossessed()
        {
            _entity.MarkAsPossessed();
            HealthChangedFromPrediction message = CreatePredictionMessage(100, 70);

            _visuals.Handle(message);
            AssertDamageTextNotPlayed();
        }

        [Test]
        public void Handle_Prediction_DamageTaken_PossessedEntity_PassesTrueToAudioPlayer()
        {
            _entity.MarkAsPossessed();

            HealthChangedFromPrediction message = CreatePredictionMessage(100, 70);
            _visuals.Handle(message);

            AssertHitSoundPlayed(isPossessed: true);
        }

        [Test]
        public void Handle_Prediction_FatalDamage_WithPredictionAllowed_PredictsDestroy()
        {
            _config.SetPrivateField("_allowDestroyPrediction", true);

            HealthChangedFromPrediction message = CreatePredictionMessage(30, 0);
            _visuals.Handle(message);

            AssertDestroySoundPlayed(_config.DestroyByOtherSoundPath);
            AssertDestroyVisualPlayed();
        }

        [Test]
        public void Handle_Prediction_FatalDamage_PreventsSubsequentServerDestroy()
        {
            _config.SetPrivateField("_allowDestroyPrediction", true);

            HealthChangedFromPrediction predictionMessage = CreatePredictionMessage(30, 0);
            _visuals.Handle(predictionMessage);

            _audioPlayer.ClearReceivedCalls();
            _visualService.ClearReceivedCalls();

            HealthChangedFromServer serverMessage = CreateServerMessage(authoritativeHealth: 0);
            _visuals.Handle(serverMessage);

            _audioPlayer
                .DidNotReceiveWithAnyArgs()
                .PlayOneShotAttached(default, default, default);

            _visualService
                .DidNotReceiveWithAnyArgs()
                .Play<ParticleSystem>(default, default, default);
        }

        [Test]
        public void Handle_Prediction_FatalDamage_WithPredictionDisabled_DoesNotPredictDestroy()
        {
            _config.SetPrivateField("_allowDestroyPrediction", false);

            HealthChangedFromPrediction message = CreatePredictionMessage(30, 0);
            _visuals.Handle(message);

            AssertDestroySoundNotPlayed(_config.DestroyByOtherSoundPath);
            AssertHitSoundPlayed(isPossessed: false);
        }

        [Test]
        public void Handle_Server_HealthAboveZero_DoesNothing()
        {
            HealthChangedFromServer message = CreateServerMessage(authoritativeHealth: 50);
            _visuals.Handle(message);

            _audioPlayer
                .DidNotReceiveWithAnyArgs()
                .PlayOneShotAttached(default, default, default);

            _visualService
                .DidNotReceiveWithAnyArgs()
                .Play<ParticleSystem>(default, default, default);
        }

        [Test]
        public void Handle_Server_FatalHealth_WhenNotPreviouslyPredicted_PredictsDestroy()
        {
            HealthChangedFromServer message = CreateServerMessage(authoritativeHealth: 0);
            _visuals.Handle(message);

            AssertDestroySoundPlayed(_config.DestroyByOtherSoundPath);
            AssertDestroyVisualPlayed();
        }

        [Test]
        public void PredictDestroy_LocalPlayer_PlaysDestroyBySelfSound()
        {
            IEntityBehaviour localPlayer = Substitute.For<IEntityBehaviour>();
            _entityService.GetPossessedEntity().Returns(localPlayer);

            HealthChangedFromServer message = CreateServerMessage(
                authoritativeHealth: 0,
                target: localPlayer,
                from: Substitute.For<IEntityBehaviour>());

            _visuals.Handle(message);

            AssertDestroySoundPlayed(_config.DestroyBySelfSoundPath);
            AssertDestroySoundNotPlayed(_config.DestroyByPlayerSoundPath);
            AssertDestroySoundNotPlayed(_config.DestroyByOtherSoundPath);
        }

        [Test]
        public void PredictDestroy_FromLocalPlayer_PlaysDestroyByPlayerSound()
        {
            IEntityBehaviour localPlayer = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            _entityService.GetPossessedEntity().Returns(localPlayer);

            HealthChangedFromServer message = CreateServerMessage(
                authoritativeHealth: 0,
                target: target,
                from: localPlayer);

            _visuals.Handle(message);

            AssertDestroySoundPlayed(_config.DestroyByPlayerSoundPath);
            AssertDestroySoundNotPlayed(_config.DestroyBySelfSoundPath);
            AssertDestroySoundNotPlayed(_config.DestroyByOtherSoundPath);
        }

        [Test]
        public void PredictDestroy_FromOther_PlaysDestroyByOtherSound()
        {
            IEntityBehaviour localPlayer = Substitute.For<IEntityBehaviour>();
            _entityService.GetPossessedEntity().Returns(localPlayer);

            HealthChangedFromServer message = CreateServerMessage(
                authoritativeHealth: 0,
                target: Substitute.For<IEntityBehaviour>(),
                from: Substitute.For<IEntityBehaviour>());

            _visuals.Handle(message);
            AssertDestroySoundPlayed(_config.DestroyByOtherSoundPath);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void PredictDestroy_DestroySoundPathEmptyOrNull_SkipsAudio(string path)
        {
            _config.SetPrivateField("_destroyByOtherSoundPath", path);

            HealthChangedFromServer message = CreateServerMessage(authoritativeHealth: 0);
            _visuals.Handle(message);

            _audioPlayer
                .DidNotReceiveWithAnyArgs()
                .PlayOneShotAttached(default, default, default);
        }

        [Test]
        public void PredictDestroy_DestroyVisualTypeNone_SkipsVisual()
        {
            _config.SetPrivateField("_destroyVisualType", VisualType.None);

            HealthChangedFromServer message = CreateServerMessage(authoritativeHealth: 0);
            _visuals.Handle(message);

            AssertDestroyVisualNotPlayed();
        }

        [Test]
        public void Awake_RegistersWithHealthBehaviour_ReceivesMessagesFromHealthBehaviour()
        {
            HealthChangedFromServer message = CreateServerMessage(authoritativeHealth: 0);
            _health.EmitMessage(message);

            AssertDestroySoundPlayed(_config.DestroyByOtherSoundPath);
        }

        private void SetupParticleSystem()
        {
            _particleSystemGo = new GameObject("TestParticleSystem");
            _particleSystem = _particleSystemGo.AddComponent<ParticleSystem>();

            _visualService
                .Play<ParticleSystem>(
                    Arg.Any<VisualType>(),
                    Arg.Any<float2>(),
                    Arg.Any<Angle>())
                .Returns(_particleSystem);
        }

        private void SetupDamagePreviewText()
        {
            _damageTextGo = new GameObject("TestDamagePreviewText");
            _damagePreviewText = _damageTextGo.AddComponent<DamagePreviewText>();

            var uiText = _damageTextGo.AddComponent<Text>();
            var anim = _damageTextGo.AddComponent<Animation>();
            var clip = new AnimationClip { legacy = true };
            anim.AddClip(clip, "DamagePreview_Popup");

            _damagePreviewText.SetPrivateField("_text", uiText);
            _damagePreviewText.SetPrivateField("_animation", anim);
            _damagePreviewText.SetPrivateField("_color", new Gradient());

            _visualService
                .Play<DamagePreviewText>(
                    Arg.Any<VisualType>(),
                    Arg.Any<float2>(),
                    Arg.Any<Angle>())
                .Returns(_damagePreviewText);
        }

        private void AssertNothingTriggered()
        {
            _audioPlayer
                .DidNotReceiveWithAnyArgs()
                .PlayOneShotAttached(default, default, default);

            _visualService
                .DidNotReceiveWithAnyArgs()
                .Play<ParticleSystem>(default, default, default);

            _visualService
                .DidNotReceiveWithAnyArgs()
                .Play<DamagePreviewText>(default, default, default);
        }

        private void AssertHitSoundPlayed(bool isPossessed)
        {
            _hitSound.Received(1).AttachTo(isPossessed
                ? _cameraMain.Get().ShakeTransform
                : _health.Transform);

            _hitSound.Received(1).Play();
        }

        private void AssertHitVisualPlayed()
        {
            _visualService
                .Received(1)
                .Play<ParticleSystem>(_config.DestoryVisualType, _health.Transform.WorldPosition);
        }

        private void AssertHitVisualNotPlayed()
        {
            _visualService
                .DidNotReceive()
                .Play<ParticleSystem>(
                    Arg.Any<VisualType>(), Arg.Any<float2>(), Arg.Any<Angle>());
        }

        private void AssertDamageTextPlayed()
        {
            _visualService
                .Received(1)
                .Play<DamagePreviewText>(VisualType.DamageText);
        }

        private void AssertDamageTextNotPlayed()
        {
            _visualService
                .DidNotReceive()
                .Play<DamagePreviewText>(Arg.Any<VisualType>());
        }

        private void AssertDestroySoundPlayed(string expectedPath)
        {
            _audioPlayer
                .Received(1)
                .PlayOneShotAttached(expectedPath, _health.Transform, Arg.Any<bool>());
        }

        private void AssertDestroySoundNotPlayed(string path)
        {
            _audioPlayer
                .DidNotReceive()
                .PlayOneShotAttached(path, Arg.Any<ITransform>(), Arg.Any<bool>());
        }

        private void AssertDestroyVisualPlayed()
        {
            _visualService
                .Received(1)
                .Play<ParticleSystem>(_config.DestoryVisualType, _health.Transform.WorldPosition);
        }

        private void AssertDestroyVisualNotPlayed()
        {
            _visualService
                .DidNotReceive()
                .Play<ParticleSystem>(
                    Arg.Any<VisualType>(), Arg.Any<float2>(), Arg.Any<Angle>());
        }

        private static HealthChangedFromPrediction CreatePredictionMessage(
            float oldHealth,
            float newHealth,
            IEntityBehaviour target = null,
            IEntityBehaviour from = null)
        {
            return new HealthChangedFromPrediction
            {
                OldState = new HealthState(oldHealth, 100),
                NewState = new HealthState(newHealth, 100),
                Target = target,
                From = from
            };
        }

        private static HealthChangedFromServer CreateServerMessage(
            float authoritativeHealth,
            IEntityBehaviour target = null,
            IEntityBehaviour from = null)
        {
            return new HealthChangedFromServer
            {
                AuthoritativeState = new HealthState(authoritativeHealth, 100),
                Target = target,
                From = from
            };
        }

        private static EntityVisualConfig CreateConfig(
            VisualType hitVisualType = VisualType.Explosion_1,
            string hitSoundPath = "sound/hit",
            VisualType destroyVisualType = VisualType.Explosion_1,
            bool allowDestroyPrediction = true,
            string destroyBySelfSoundPath = "sound/destroy_self",
            string destroyByPlayerSoundPath = "sound/destroy_player",
            string destroyByOtherSoundPath = "sound/destroy_other")
        {
            var config = ScriptableObject.CreateInstance<EntityVisualConfig>();

            config.SetPrivateField("_hitVisualType", hitVisualType);
            config.SetPrivateField("_hitSoundPath", hitSoundPath);
            config.SetPrivateField("_destroyVisualType", destroyVisualType);
            config.SetPrivateField("_allowDestroyPrediction", allowDestroyPrediction);
            config.SetPrivateField("_destroyBySelfSoundPath", destroyBySelfSoundPath);
            config.SetPrivateField("_destroyByPlayerSoundPath", destroyByPlayerSoundPath);
            config.SetPrivateField("_destroyByOtherSoundPath", destroyByOtherSoundPath);

            return config;
        }
    }
}
