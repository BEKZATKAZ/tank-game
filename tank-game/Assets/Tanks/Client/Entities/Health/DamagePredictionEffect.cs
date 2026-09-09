using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Audio;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using UnityEngine;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePredictionEffect :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<HealthChangedFromPrediction>
    {
        public DamagePredictionEffect(
            IAudioPlayer audioPlayer,
            ICameraShakeService cameraShake)
        {
            _audioPlayer = audioPlayer;
            _cameraShake = cameraShake;
        }

        private readonly IAudioPlayer _audioPlayer;
        private readonly ICameraShakeService _cameraShake;

        private Dictionary<SpriteRenderer, Color> _sprites;
        private float _lastTime;

        public async void Handle(HealthChangedFromPrediction message)
        {
            if (message.OldState.Current < message.NewState.Current) return;

            _lastTime = Time.time;
            float currentTime = _lastTime;

            foreach ((SpriteRenderer renderer, Color color) in _sprites)
            {
                renderer.color = Color.Lerp(Color.red, color, 0.5f);
            }

            _audioPlayer.PlayOneShotAttached(
                path: "event:/SFX/DamageTaken",
                target: Transform,
                useDefault: Behaviour.Entity.IsPossessed);

            bool isLocalPlayer = message.Target.IsPossessed;
            bool isFromLocalPlayer = message.From?.IsPossessed ?? false;

            if (isLocalPlayer || isFromLocalPlayer)
            {
                _cameraShake.AddShake(new CameraImpulse(
                    position: Transform.WorldPosition,
                    amplitude: 0.8f,
                    frequency: 4,
                    radius: isLocalPlayer ? 1000 : isFromLocalPlayer ? 20 : 5,
                    duration: 0.9f));
            }

            await UniTask.Delay(150);

            // Prevent conflicts if other damage is taken
            if (currentTime != _lastTime) return;

            // Prevent effect from applying when the entity is destroyed
            if (Transform.IsValid == false) return;

            foreach ((SpriteRenderer renderer, Color color) in _sprites)
            {
                renderer.color = color;
            }
        }

        protected override void OnInitialize()
        {
            _sprites = Behaviour.GetAllSpriteRenderers().ToDictionary(x => x, x => x.color);
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
