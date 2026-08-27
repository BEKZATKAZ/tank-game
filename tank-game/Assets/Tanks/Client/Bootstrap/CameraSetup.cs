using Cysharp.Threading.Tasks;
using System;
using Tanks.Audio;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using UnityEngine;

namespace Tanks.Client.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>,
        INetworkMessageHandler<GameStarted>
    {
        public CameraSetup(
            IAudioPlayer audioPlayer,
            ICameraMainService camera,
            ICameraFollowService follow,
            IInputService<ManualDriveInput> driveInput,
            IInputService<TurretInput> turretInput)
        {
            _audioPlayer = audioPlayer;
            _camera = camera;
            _follow = follow;
            _driveInput = driveInput;
            _turretInput = turretInput;
        }

        private readonly IAudioPlayer _audioPlayer;
        private readonly ICameraMainService _camera;
        private readonly ICameraFollowService _follow;
        private readonly IInputService<ManualDriveInput> _driveInput;
        private readonly IInputService<TurretInput> _turretInput;

        private Bounds _mapBounds;

        public void Handle(TilemapGenerated message)
        {
            _mapBounds = message.Bounds;
            _driveInput.Disable();
            _turretInput.Disable();
        }

        public async void Handle(GameStarted message)
        {
            var player = Behaviour.NetworkManager.LocalClient.PlayerObject;

            _audioPlayer.SetDefault(_camera.Get().Transform);
            _follow.SetBounds(_mapBounds);
            _follow.SetFollowTarget(new UnityTransform(player.transform));

            await UniTask.Delay(TimeSpan.FromSeconds(message.StartDelaySeconds));

            _driveInput.Enable();
            _turretInput.Enable();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<TilemapGenerated>(this);
            Behaviour.RegisterMessageHandler<GameStarted>(this);
        }
    }
}
