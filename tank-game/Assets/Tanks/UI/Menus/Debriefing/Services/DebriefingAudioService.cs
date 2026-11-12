using Tanks.Audio;
using Unity.Mathematics;

namespace Tanks.UI.Menus.Debriefing.Services
{
    public sealed class DebriefingAudioService : IDebriefingAudioService
    {
        public DebriefingAudioService(IAudioFactory factory, IAudioPlayer player)
        {
            _factory = factory;
            _player = player;
        }

        private readonly IAudioFactory _factory;
        private readonly IAudioPlayer _player;

        private IAudioInstance _counting;
        private string _statPath;
        private string _levelPath;
        private float _maxPitch = 1.25f;
        private float _pitch = 1;

        public void Configure(string statPath, string levelPath, float maxPitch)
        {
            Stop();
            _statPath = statPath;
            _levelPath = levelPath;
            _maxPitch = math.max(1, maxPitch);
            _pitch = 1;
        }

        public void SetProgress(float progress)
        {
            _pitch = math.lerp(1, _maxPitch, math.saturate(progress));
            _counting?.SetPitch(_pitch);
        }

        public void SetCounting(bool active)
        {
            Stop();
            if (active == false || string.IsNullOrEmpty(_statPath)) return;
            _counting = _factory.CreateInstance(_statPath);
            _counting.SetPitch(_pitch);
            _counting.Play();
        }

        public void LevelUp()
        {
            if (string.IsNullOrEmpty(_levelPath) == false)
            {
                _player.PlayOneShot(_levelPath);
            }
        }

        public void Stop()
        {
            if (_counting == null) return;
            _counting.Stop(false);
            _counting.Dispose();
            _counting = null;
        }
    }
}
