using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Tanks.Audio;
using Tanks.Client.Entities.Health;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Entities.Runtime
{
    [RequireComponent(typeof(HealthBehaviour))]
    public sealed class EntityVisuals :
        MonoBehaviour,
        INetworkMessageHandler<HealthChangedFromServer>,
        INetworkMessageHandler<HealthChangedFromPrediction>
    {
        [Expandable]
        [Required]
        [SerializeField]
        private EntityVisualConfig _config;

        [SerializeField]
        private ParticleSystem _customHitParticle;

        [SerializeField]
        private ParticleSystem _customDestroyParticle;

        [SerializeField]
        private SpriteRenderer[] _bodySprites;

        private IHealthBehaviour _health;
        private IVisualService _visualService;
        private IAudioPlayer _audioPlayer;
        private IClientEntityService _entityService;
        private ICameraMainService _cameraMain;
        private ICameraShakeService _cameraShake;
        private bool _hasPredictedDestroy;
        private float _lastHitTime;

        private ITransform _hitParticleTransform;
        private ITransform _destroyParticleTransform;
        private IAudioInstance _hitSound;

        [Inject]
        public void Construct(
            IVisualService visualService,
            IAudioPlayer audioPlayer,
            IAudioFactory audioFactory,
            IClientEntityService entityService,
            ICameraMainService cameraMain,
            ICameraShakeService shakeService)
        {
            _visualService = visualService;
            _audioPlayer = audioPlayer;
            _entityService = entityService;
            _cameraMain = cameraMain;
            _cameraShake = shakeService;

            if (string.IsNullOrWhiteSpace(_config.HitSoundPath)) return;
            _hitSound = audioFactory.CreateInstance(_config.HitSoundPath);
        }

        public void Handle(HealthChangedFromServer message)
        {
            if (message.AuthoritativeState.Current > 0) return;
            if (_hasPredictedDestroy) return;

            PredictDestroy(message.Target, message.From);
        }

        public void Handle(HealthChangedFromPrediction message)
        {
            // Discard heals and reduntant updates
            if (message.OldState.Current <= message.NewState.Current) return;

            FlashSpritesAsync(message).Forget();
            ShowDamagePreviewText(message);

            if (message.NewState.Current <= 0 && _config.AllowDestroyPrediction)
            {
                _hasPredictedDestroy = true;
                PredictDestroy(message.Target, message.From);
                return;
            }

            PlayHitVisuals();
        }

        private void PlayHitVisuals()
        {
            if (_hitSound != null)
            {
                _hitSound.AttachTo(_health.Entity.IsPossessed
                    ? _cameraMain.Get().ShakeTransform
                    : _health.Transform);

                _hitSound.Play();
            }

            if (_config.HitVisualType != VisualType.None)
            {
                _visualService
                    .Play<ParticleSystem>(_config.DestoryVisualType, _health.Transform.WorldPosition)
                    .Play(true);
            }

            if (_customHitParticle) _customHitParticle.Play(true);
        }

        private void ShowDamagePreviewText(HealthChangedFromPrediction message)
        {
            if (_health.Entity.IsPossessed) return;

            int previous = message.OldState.Current;
            int current = message.NewState.Current;

            DamagePreviewText text = _visualService.Play<DamagePreviewText>(VisualType.DamageText);
            text.Initialize(previous - current, message.NewState, _health.Transform);
            text.Animate();
        }

        private async UniTaskVoid FlashSpritesAsync(HealthChangedFromPrediction message)
        {
            if (_bodySprites is not { Length: > 0 }) return;

            float currentTime = Time.time;
            _lastHitTime = currentTime;

            foreach (SpriteRenderer renderer in _bodySprites)
            {
                renderer.material.SetFloat("_HitAlpha", 1);
            }

            bool isLocalPlayer = message.Target.IsPossessed;
            bool isFromLocalPlayer = message.From?.IsPossessed ?? false;

            if (_health.OwnerClientId > 0 && (isLocalPlayer || isFromLocalPlayer))
            {
                _cameraShake.AddShake(new CameraImpulse(
                    position: _health.Transform.WorldPosition,
                    amplitude: 0.8f,
                    frequency: 4,
                    radius: isLocalPlayer ? 1000 : isFromLocalPlayer ? 20 : 5,
                    duration: 0.9f));
            }

            await UniTask.Delay(50);

            // Prevent conflicts if other damage is taken
            if (currentTime != _lastHitTime) return;

            // Prevent effect from applying when the entity is destroyed
            if (this == false) return;

            foreach (SpriteRenderer renderer in _bodySprites)
            {
                renderer.material.SetFloat("_HitAlpha", 0);
            }
        }

        private void PredictDestroy(IEntityBehaviour target, IEntityBehaviour from)
        {
            IEntityBehaviour localPlayer = _entityService.GetPossessedEntity();

            bool isLocalPlayer = target == localPlayer;
            bool isFromLocalPlayer = from == localPlayer;

            if (_health.OwnerClientId > 0 && (isLocalPlayer || isFromLocalPlayer))
            {
                _cameraShake.AddShake(new CameraImpulse(
                    position: _health.Transform.WorldPosition,
                    amplitude: 3,
                    frequency: 1.5f,
                    radius: isLocalPlayer ? 1000 : isFromLocalPlayer ? 40 : 10,
                    duration: 3));

                _cameraShake.AddShake(new CameraImpulse(
                    position: _health.Transform.WorldPosition,
                    amplitude: 0.4f,
                    frequency: 30,
                    radius: isLocalPlayer ? 1000 : isFromLocalPlayer ? 20 : 5,
                    duration: 1.2f));
            }

            string path;

            if (isLocalPlayer) path = _config.DestroyBySelfSoundPath;
            else if (isFromLocalPlayer) path = _config.DestroyByPlayerSoundPath;
            else path = _config.DestroyByOtherSoundPath;

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                // Playback from the main camera if the local player watches this entity
                if (_health.Entity.IsPossessed)
                {
                    _audioPlayer.PlayOneShotAttached(path, _health.Transform, useDefault: true);
                }
                else
                {
                    _audioPlayer.PlayOneShot(path, _health.Transform.WorldPosition);
                }
            }

            if (_config.DestoryVisualType != VisualType.None)
            {
                _visualService
                    .Play<ParticleSystem>(_config.DestoryVisualType, _health.Transform.WorldPosition)
                    .Play(true);
            }

            if (_customDestroyParticle) _customDestroyParticle.Play(true);
        }

        private void Awake()
        {
            if (_customHitParticle)
            {
                _hitParticleTransform = new UnityTransform(_customHitParticle.transform);
                _hitParticleTransform.SetParent(null);
            }

            if (_customDestroyParticle)
            {
                _destroyParticleTransform = new UnityTransform(_customDestroyParticle.transform);
                _destroyParticleTransform.SetParent(null);
            }

            _health = GetComponent<HealthBehaviour>();
            _health.RegisterMessageHandler<HealthChangedFromPrediction>(this);
            _health.RegisterMessageHandler<HealthChangedFromServer>(this);
        }

        private void LateUpdate()
        {
            if (_hitParticleTransform != null)
            {
                _hitParticleTransform.WorldPosition = _health.Transform.WorldPosition;
            }

            if (_destroyParticleTransform != null)
            {
                _destroyParticleTransform.WorldPosition = _health.Transform.WorldPosition;
            }
        }

        private void OnDestroy()
        {
            _hitSound?.Dispose();
        }
    }
}
