using Tanks.Shared.Adapters;
using Tanks.Shared.Effects;
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
        private IEffectOwnerBehaviour _effectOwner;

        protected override void Awake()
        {
            base.Awake();
            _rotation = new TankTurretRotation(Transform, Angle.FromDegrees(1200));
            _effectOwner = GetComponentInParent<EffectOwnerBehaviour>();
        }

        public void Tick(float2 lookAtDirection, float deltaTime)
        {
            if (math.length(lookAtDirection) < 0.01f) return;
            if (_effectOwner.Effects.ContainsKey(EffectType.Frozen)) return;

            _rotation.TurnAt(lookAtDirection, deltaTime);
        }
    }
}
