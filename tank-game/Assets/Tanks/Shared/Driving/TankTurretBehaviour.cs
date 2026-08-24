using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Driving
{
    public interface ITankTurretBehaviour : INetworkBehaviourExtended
    {
        void Tick(float2 lookAtDirection, float deltaTime);
    }

    public class TankTurretBehaviour : NetworkBehaviourExtended, ITankTurretBehaviour
    {
        private TankTurretRotation _rotation;

        protected override void Awake()
        {
            base.Awake();
            _rotation = new TankTurretRotation(Transform, Angle.FromDegrees(1200));
        }

        public void Tick(float2 lookAtDirection, float deltaTime)
        {
            if (math.length(lookAtDirection) < 0.01f) return;
            _rotation.TurnAt(lookAtDirection, deltaTime);
        }
    }
}
