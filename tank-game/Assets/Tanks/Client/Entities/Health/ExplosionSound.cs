using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Entities.Health
{
    public sealed class ExplosionSound :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<Died>,
        INetworkMessageHandler<DiedLocal>
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
            string path = _other;

            if (message.From != null)
            {
                IEntity localPlayer = _entityService.GetLocalPlayer();
                path = message.From == localPlayer ? _killed : _other;
            }

            _audioPlayer.PlayOneShot(path, message.WorldPosition);
        }

        public void Handle(DiedLocal message)
        {
            _audioPlayer.PlayOneShot(_self);
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer) Behaviour.RegisterMessageHandler<DiedLocal>(this);
            else Behaviour.RegisterMessageHandler<Died>(this);
        }
    }
}
