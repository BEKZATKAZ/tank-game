// ! This is kept in the editor for testing
#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        public async void Handle(TilemapGenerated message)
        {
            // Sometimes camera setup is called before the scene is fully loaded
            await UniTask.DelayFrame(1);

            float3 size = message.Bounds.size;
            Required<Camera> camera = Camera.main;

            camera.Value.transform.position = message.Bounds.center + Vector3.back * 10;
            camera.Value.orthographicSize = math.cmin(size.xy) * 0.5f;
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
#endif
