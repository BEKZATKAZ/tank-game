using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.DangerZone
{
    public sealed class DangerZoneSound : NetworkComponent<IDangerZoneBehaviour>
    {
        public DangerZoneSound(
            IAudioPlayer audioPlayer,
            IClientEntityService clientEntityService)
        {
            _audioPlayer = audioPlayer;
            _clientEntityService = clientEntityService;
        }

        private readonly IAudioPlayer _audioPlayer;
        private readonly IClientEntityService _clientEntityService;
        private bool _isOutsideSafeArea;

        protected override void OnLateUpdate(float deltaTime)
        {
            IEntityBehaviour possessed = _clientEntityService.GetPossessedEntity();

            if (possessed is not { IsValid: true }) return;
            float2 position = possessed.Transform.WorldPosition;

            // "Outside" because Rect measures inwards, not outwards
            bool isOutsideSafeArea = Behaviour.CurrentState
                .Overlaps(new Rect(position.x, position.y, 0.01f, 0.01f));

            if (isOutsideSafeArea == _isOutsideSafeArea) return;
            _isOutsideSafeArea = isOutsideSafeArea;

            if (_isOutsideSafeArea) return;
            _audioPlayer.PlayOneShot("event:/SFX/DangerZone_Entered");
        }
    }
}
