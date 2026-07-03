using Unity.Mathematics;

namespace Tanks.Shared.Health
{
    public interface IDamage
    {
        float Evaluate(float distance);
    }

    public sealed class NearFarDamage : IDamage
    {
        public NearFarDamage(
            float nearAmount, float farAmount,
            float minDistance, float maxDistance)
        {
            _nearAmount = nearAmount;
            _farAmount = farAmount;

            _minDistance = minDistance;
            _maxDistance = maxDistance;
        }

        private readonly float _nearAmount;
        private readonly float _farAmount;

        private readonly float _minDistance;
        private readonly float _maxDistance;

        public float Evaluate(float distance)
        {
            float alpha = math.saturate(math.unlerp(_minDistance, _maxDistance, distance));
            return math.lerp(_nearAmount, _farAmount, alpha);
        }
    }
}