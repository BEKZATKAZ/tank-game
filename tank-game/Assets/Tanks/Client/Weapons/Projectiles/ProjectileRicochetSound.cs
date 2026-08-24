using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Weapons
{
    public sealed class ProjectileRicochetSound :
        NetworkComponent<IProjectileBehaviour>,
        INetworkMessageHandler<ProjectileRicocheted>
    {
        public ProjectileRicochetSound(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
        }

        private readonly IAudioPlayer _audioPlayer;

        public void Handle(ProjectileRicocheted message)
        {
            float2 hitPoint = message.HitPoint;
            _audioPlayer.PlayOneShot("event:/SFX/Ricochet", hitPoint);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
