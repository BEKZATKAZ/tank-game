using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class CameraSetup :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        public void Handle(TilemapGenerated message)
        {
            float3 size = message.Bounds.size;
            Required<Camera> camera = Camera.main;

            camera.Value.transform.position = message.Bounds.center + Vector3.back * 10;
            camera.Value.orthographicSize = math.cmin(size.xy) * 0.5f;
        }

        protected override void OnInitialize()
        {
            // This is kept in the editor for testing
            if (Application.isEditor == false)
            {
                IsEnabled = false;
                return;
            }

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
