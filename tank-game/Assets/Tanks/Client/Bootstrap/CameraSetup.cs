using Cysharp.Threading.Tasks;
using System;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Bootstrap;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>,
        INetworkMessageHandler<GameStarted>
    {
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
            float3 size = _mapBounds.size;
            float3 center = _mapBounds.center;

            _follow.SetDefault(center.xy, math.cmin(size.xy) * 0.5f, forceOverride: true);
            await UniTask.Delay(TimeSpan.FromSeconds(message.StartDelaySeconds));

            var player = NetworkManager.LocalClient.PlayerObject;
            _follow.SetFollowTarget(new UnityTransform(player.transform), 15);

            _driveInput.Enable();
            _turretInput.Enable();
        }

        private void Start()
        {
            Behaviour.RegisterMessageHandler<TilemapGenerated>(this);
            Behaviour.RegisterMessageHandler<GameStarted>(this);
        }
    }
}
