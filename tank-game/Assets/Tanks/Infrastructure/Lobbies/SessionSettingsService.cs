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
        private SessionDefaultSettingsConfig _defaultSettings;

        private ISessionSettings _settings;

        public ISessionDefaultSettingsConfig DefaultSettings => _defaultSettings;

        public ISessionSettings GetSettings() => _settings;
        public void SetSettings(ISessionSettings settings) => _settings = settings;
    }
}
