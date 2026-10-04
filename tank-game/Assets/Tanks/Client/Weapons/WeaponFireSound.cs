using Tanks.Audio;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFireSound :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        public WeaponFireSound(IAudioPlayer audioPlayer, ICameraShakeService cameraShake)
        {
            _audioPlayer = audioPlayer;
            _cameraShake = cameraShake;
        }

        private readonly IAudioPlayer _audioPlayer;
        private readonly ICameraShakeService _cameraShake;

        public void Handle(WeaponUsed message)
        {
            _audioPlayer.PlayOneShotAttached(
                path: $"event:/SFX/Weapons/{message.Weapon.Value.Name}/Fire",
                target: Transform,
                useDefault: Behaviour.Entity.IsPossessed);

            IRecoilConfig recoil = message.Weapon.Value.Recoil;
            bool isLocalPlayer = Behaviour.Entity.IsPossessed;

            _cameraShake.AddShake(new CameraImpulse(
                position: Transform.WorldPosition,
                amplitude: recoil.Amount * 0.15f,
                frequency: 4,
                radius: isLocalPlayer ? 1000 : 10,
                duration: recoil.DurationSeconds * 0.75f));
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
