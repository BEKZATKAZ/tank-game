using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Client.Rendering;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Driving;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        [Inject] private readonly ICameraFollowService _follow;

        public async void Handle(TilemapGenerated message)
        {
            float3 size = message.Bounds.size;
            float3 center = message.Bounds.center;
            _follow.SetDefault(center.xy, math.cmin(size.xy) * 0.5f, forceOverride: true);

            while (NetworkManager.LocalClient.OwnedObjects.Length == 0)
            {
                await UniTask.Yield();
                if (Application.isPlaying == false) return;
            }

            var player = NetworkManager.LocalClient.OwnedObjects
                .First(x => x.TryGetComponent<TankDriveBehaviour>(out _));

            _follow.SetFollowTarget(new UnityTransform(player.transform), 15);
        }

        private void Start()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}