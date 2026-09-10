using NaughtyAttributes;
using Tanks.Core.Lobbies.Sessions;
using UnityEngine;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class SessionSettingsService : MonoBehaviour, ISessionSettingsService
    {
        [Expandable]
        [Required]
        [SerializeField]
        private SessionSettingsConfig _defaultSettings;

        private ISessionSettings _settings;

        public ISessionSettings GetSettings()
        {
            return _settings ?? _defaultSettings;
        }

        public void SetSettings(ISessionSettings settings)
        {
            _settings = settings;
        }
    }
}
