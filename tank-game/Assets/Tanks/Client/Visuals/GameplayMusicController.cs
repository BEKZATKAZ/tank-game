using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Visuals
{
    public sealed class GameplayMusicController :
        MonoBehaviour, IMessageHandler<Died>, IMessageHandler<GameStarted>
    {
        [SerializeField] private string[] _musicPaths;

        private IAudioFactory _audioFactory;
        private IMessageBus _bus;
        private IAudioInstance _music;
        private int _currentMusicIndex;

        [Inject]
        public void Inject(IAudioFactory audioFactory, IMessageBus bus)
        {
            for (int i = _musicPaths.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_musicPaths[i], _musicPaths[j]) = (_musicPaths[j], _musicPaths[i]);
            }

            _audioFactory = audioFactory;
            _bus = bus;

            _bus.RegisterHandler<Died>(this);
            _bus.RegisterHandler<GameStarted>(this);
        }

        public async void Handle(GameStarted message, IMessageSender sender)
        {
            _music = _audioFactory.CreateInstance(_musicPaths[_currentMusicIndex]);
            _music.SetVolume(0.8f);
            _currentMusicIndex = ++_currentMusicIndex % _musicPaths.Length;

            await UniTask.Delay(TimeSpan.FromSeconds(message.StartDelaySeconds));
            _music.Play();
        }

        public async void Handle(Died message, IMessageSender sender)
        {
            // Let player objects destroy before processing
            await UniTask.DelayFrame(2);

            var allClients = NetworkManager.Singleton.ConnectedClients;
            int allCount = allClients.Count;

            int eliminatedCount = allClients.Count(x => x.Value.PlayerObject == false);
            int remaining = allCount - eliminatedCount;

            if (remaining <= 1)
            {
                DisposeMusic();
                return;
            }

            float stress = math.saturate((float)eliminatedCount / (allCount - 1));
            _music.SetParameter("Stress", stress);
        }

        private void DisposeMusic()
        {
            if (_music is null) return;
            _music.Stop();
            _music.Dispose();
            _music = null;
        }

        private void OnDestroy()
        {
            if (_bus is null) return;

            _bus.UnregisterHandler<Died>(this);
            _bus.UnregisterHandler<GameStarted>(this);

            DisposeMusic();
        }
    }
}
