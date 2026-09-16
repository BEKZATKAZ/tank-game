using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class UnityServerLifetimeService : MonoBehaviour, IServerLifetimeService
    {
        [Range(30, 300)]
        [SerializeField]
        private int _startupLifetimeSeconds = 120;

        [Range(900, 7200)]
        [SerializeField]
        private int _sessionLifetimeSeconds = 3600;

        [Range(5, 15)]
        [SerializeField]
        private int _endLifetimeSeconds = 5;

        private GameStep _currentStep;
        private float _elapsedSeconds;
        private float _lifetimeSeconds;

        public bool IsTimedOut => _elapsedSeconds >= _lifetimeSeconds;

        public void SetLifetime(int startup, int session, int end)
        {
            _startupLifetimeSeconds = startup;
            _sessionLifetimeSeconds = session;
            _endLifetimeSeconds = end;
        }

        public void TrySetStep(GameStep value)
        {
            if (_currentStep == value) return;

            _currentStep = value;
            _elapsedSeconds = 0;

            _lifetimeSeconds = value switch
            {
                GameStep.Startup => _startupLifetimeSeconds,
                GameStep.Session => _sessionLifetimeSeconds,
                GameStep.End or _ => _endLifetimeSeconds
            };
        }

        private void Awake()
        {
            TrySetStep(GameStep.Startup);
        }

        private void Update()
        {
            if (_lifetimeSeconds <= 0) return;

            float previous = _elapsedSeconds;
            _elapsedSeconds = math.min(_elapsedSeconds + Time.unscaledDeltaTime, _lifetimeSeconds);

            // True only once, when lifetime exceeds
            if (previous < _lifetimeSeconds && _elapsedSeconds >= _lifetimeSeconds)
            {
                Debug.Log("Server lifetime timed out!");
                NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
                Application.Quit();
            }
        }
    }
}
