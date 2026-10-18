using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Tanks.Audio;
using Tanks.Client.Bootstrap;
using Tanks.Client.Input;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Visuals
{
    public sealed class GameplayMusicController :
        MonoBehaviour,
        IMessageHandler<Died>,
        IMessageHandler<TilemapGenerated>,
        IMessageHandler<GameStartedFromClient>
    {
        [SerializeField] private string[] _musicPaths;

        private IAudioFactory _audioFactory;
        private IMessageBus _bus;
        private IAudioInstance _music;
        private IStressService _stress;
        private IInputService<ManualDriveInput> _driveInput;
        private IInputService<TurretInput> _turretInput;
        private int _currentMusicIndex = 0;

        [Inject]
        public void Inject(
            IAudioFactory audioFactory,
            IMessageBus bus,
            IStressService stress,
            IInputService<ManualDriveInput> driveInput,
            IInputService<TurretInput> turretInput)
        {
            _audioFactory = audioFactory;
            _bus = bus;
            _stress = stress;
            _driveInput = driveInput;
            _turretInput = turretInput;

            _bus.RegisterHandler<Died>(this);
            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            if (_currentMusicIndex >= 0) return;

            // Sync music
            var random = new Unity.Mathematics.Random(message.Settings.Seed ^ 31);

            for (int i = _musicPaths.Length - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                (_musicPaths[i], _musicPaths[j]) = (_musicPaths[j], _musicPaths[i]);
            }
        }

        public async void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            _stress.ResetStress();

            // Play only at the beginning of the first round
            if (_music != null) return;

            await UniTask.Delay(TimeSpan.FromSeconds(message.AdaptedStartDelaySeconds));
            PlayNext();
        }

        public async void Handle(Died message, IMessageSender sender)
        {
            // Discard non-player entities
            if (message.Target.GetBehaviour<ITeamParticipantBehaviour>() is null) return;

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

            _driveInput.Disable();
            _turretInput.Disable();

            DisposeMusic();
            PlayNext();
            return;
        }

        private void PlayNext()
        {
            _music = _audioFactory.CreateInstance(_musicPaths[_currentMusicIndex]);
            _music.SetVolume(0.8f);

            _currentMusicIndex = ++_currentMusicIndex % _musicPaths.Length;
            _stress.ResetStress();

            _music.Play();
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
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus.UnregisterHandler<GameStartedFromClient>(this);

            DisposeMusic();
        }
    }
}
