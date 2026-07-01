using Tanks.Shared;
using Tanks.Shared.Bootstrap;
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
            Camera camera = Camera.main;

            camera.transform.position = message.Bounds.center + Vector3.back * 10;
            camera.orthographicSize = math.cmin(size.xy) * 0.5f;
        }

        protected override void Awake()
        {
            base.Awake();

            // This is kept in the editor for testing
            if (Application.isEditor == false)
            {
                enabled = false;
                return;
            }

            Behaviour.RegisterMessageHandler(this);
        }
    }
}