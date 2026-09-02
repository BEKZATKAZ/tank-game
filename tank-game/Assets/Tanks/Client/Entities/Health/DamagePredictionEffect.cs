using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using UnityEngine;

namespace Tanks.Client.Entities.Health
{
    public class DamagePredictionEffect :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<HealthChangedFromPrediction>
    {
        public DamagePredictionEffect(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
        }

        private readonly IAudioPlayer _audioPlayer;

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
