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
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>,
        INetworkMessageHandler<GameStarted>
    {
        [Inject] private readonly IAudioPlayer _audioPlayer;
        [Inject] private readonly ICameraMainService _camera;
        [Inject] private readonly ICameraFollowService _follow;
        [Inject] private readonly IInputService<ManualDriveInput> _driveInput;
        [Inject] private readonly IInputService<TurretInput> _turretInput;

        private Bounds _mapBounds;

        public void Handle(TilemapGenerated message)
        {
            _mapBounds = message.Bounds;
            _driveInput.Disable();
            _turretInput.Disable();
        }

        public async void Handle(GameStarted message)
        {
            _follow.SetDefault(_mapBounds, forceOverride: true);
            _audioPlayer.SetDefault(_camera.Get().Transform);

            await UniTask.Delay(TimeSpan.FromSeconds(message.StartDelaySeconds));

            var player = Behaviour.NetworkManager.LocalClient.PlayerObject;
            _follow.SetFollowTarget(new UnityTransform(player.transform), 15);

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
