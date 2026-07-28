using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using VContainer;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFireSound :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        [Inject] private readonly IAudioPlayer _audioPlayer;

        public void Handle(WeaponUsed message)
        {
            _audioPlayer.PlayOneShotAttached(
                path: "event:/SFX/Shoot",
                target: Behaviour.Transform,
                useDefault: Behaviour.IsLocalPlayer);
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
