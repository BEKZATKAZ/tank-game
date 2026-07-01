using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Driving
{
    public class TankTurretBehaviour : NetworkBehaviourExtended
    {
        private TankTurretRotation _rotation;

        protected override void Awake()
        {
            base.Awake();
            _rotation = new TankTurretRotation(Transform, Angle.FromDegrees(1200));
        }

        public void Tick(float2 lookAtDirection, bool fire, float deltaTime)
        {
            if (math.length(lookAtDirection) >= 0.01f)
            {
                _rotation.TurnAt(lookAtDirection, deltaTime);
            }
        }
    }
}