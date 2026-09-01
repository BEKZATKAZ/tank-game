using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Entities.Health
{
    public sealed class ExplosionSound :
        NetworkComponent<IHealthBehaviour>, INetworkMessageHandler<Died>
    {
        public ExplosionSound(IClientEntityService entityService, IAudioPlayer audioPlayer)
        {
            _entityService = entityService;
            _audioPlayer = audioPlayer;
        }

        private readonly IClientEntityService _entityService;
        private readonly IAudioPlayer _audioPlayer;

        private static readonly string _other = "event:/SFX/Explosion/Explosion_Other";
        private static readonly string _killed = "event:/SFX/Explosion/Explosion_Killed";
        private static readonly string _self = "event:/SFX/Explosion/Explosion_Self";

        public void Handle(Died message)
        {
            IEntityBehaviour localPlayer = _entityService.GetPossessedPlayer();

            if (message.Target == localPlayer)
            {
                _audioPlayer.PlayOneShot(_self, message.WorldPosition);
                return;
            }

            if (message.From == localPlayer)
            {
                _audioPlayer.PlayOneShot(_killed, message.WorldPosition);
                return;
            }

            _audioPlayer.PlayOneShot(_other, message.WorldPosition);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
