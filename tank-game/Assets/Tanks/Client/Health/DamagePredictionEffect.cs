using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Health;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Health
{
    public class DamagePredictionEffect :
        NetworkComponent<HealthBehaviour>,
        INetworkMessageHandler<DamageTaken>
    {
        [Inject] private readonly IAudioPlayer _audioPlayer;

        private Dictionary<SpriteRenderer, Color> _sprites;
        private float _lastTime;

        public async void Handle(DamageTaken message)
        {
            _lastTime = Time.time;

            float currentTime = _lastTime;

            foreach ((SpriteRenderer renderer, Color color) in _sprites)
            {
                renderer.color = Color.Lerp(Color.red, color, 0.5f);
            }

            _audioPlayer.PlayOneShotAttached(
                path: "event:/SFX/DamageTaken",
                target: Behaviour.Transform,
                useDefault: Behaviour.IsLocalPlayer);

            await UniTask.Delay(150);

            // Prevent conflicts if other damage is taken
            if (currentTime != _lastTime) return;

            // Prevent effect from applying when the entity is destroyed
            if (this == false) return;

            foreach ((SpriteRenderer renderer, Color color) in _sprites)
            {
                renderer.color = color;
            }
        }

        private void Start()
        {
            _sprites = GetComponentsInChildren<SpriteRenderer>()
                .ToDictionary(x => x, x => x.color);

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
