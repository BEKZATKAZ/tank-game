using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFireSound :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        public WeaponFireSound(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
        }

        private readonly IAudioPlayer _audioPlayer;

        public void Handle(WeaponUsed message)
        {
            _audioPlayer.PlayOneShotAttached(
                path: "event:/SFX/Shoot",
                target: Transform,
                useDefault: Behaviour.Entity.IsPossessed);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
