using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Tanks.Audio;
using Tanks.Client.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Visuals
{
    public sealed class GameplayMusicController :
        MonoBehaviour, IMessageHandler<Died>, IMessageHandler<GameStartedFromClient>
    {
        [SerializeField] private string[] _musicPaths;

        private IAudioFactory _audioFactory;
        private IMessageBus _bus;
        private IAudioInstance _music;
        private IStressService _stress;
        private int _currentMusicIndex;

        [Inject]
        public void Inject(IAudioFactory audioFactory, IMessageBus bus, IStressService stress)
        {
            for (int i = _musicPaths.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_musicPaths[i], _musicPaths[j]) = (_musicPaths[j], _musicPaths[i]);
            }

            _audioFactory = audioFactory;
            _bus = bus;
            _stress = stress;

            _bus.RegisterHandler<Died>(this);
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        public async void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            _music = _audioFactory.CreateInstance(_musicPaths[_currentMusicIndex]);
            _music.SetVolume(0.8f);

            _currentMusicIndex = ++_currentMusicIndex % _musicPaths.Length;
            _stress.ResetStress();

            await UniTask.Delay(TimeSpan.FromSeconds(message.AdaptedStartDelaySeconds));
            _music.Play();
        }

        public async void Handle(Died message, IMessageSender sender)
        {
            // Let player objects destroy before processing
            await UniTask.DelayFrame(2);

            var allClients = NetworkManager.Singleton.ConnectedClients;
            int eliminatedCount = allClients.Count(x => x.Value.PlayerObject == false);
            int remaining = allClients.Count - eliminatedCount;

            if (remaining > 2)
            {
                _stress.AddStress(1f / allClients.Count);
                return;
            }

            DisposeMusic();
            return;
        }

        private void DisposeMusic()
        {
            if (_music is null) return;
            _music.Stop();
            _music.Dispose();
            _music = null;
        }

        private void Update()
        {
            if (_music is null) return;
            _music.SetParameter("Stress", _stress.Stress);
        }

        private void OnDestroy()
        {
            if (_bus is null) return;

            _bus.UnregisterHandler<Died>(this);
            _bus.UnregisterHandler<GameStartedFromClient>(this);

            DisposeMusic();
        }
    }
}
