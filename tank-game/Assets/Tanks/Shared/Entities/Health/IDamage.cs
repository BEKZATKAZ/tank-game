using Tanks.Core.Helpers;
using Unity.Mathematics;

namespace Tanks.Shared.Entities.Health
{
    public interface IDamage
    {
        PositiveFloat Evaluate(PositiveFloatOrZero distance);
    }

    public sealed class NearFarDamage : IDamage
    {
        public NearFarDamage(
            PositiveFloat nearAmount, PositiveFloat farAmount,
            PositiveFloat minDistance, PositiveFloat maxDistance)
        {
            _nearAmount = nearAmount;
            _farAmount = farAmount;

            _minDistance = minDistance;
            _maxDistance = maxDistance;
        }

        private readonly PositiveFloat _nearAmount;
        private readonly PositiveFloat _farAmount;

        private readonly PositiveFloat _minDistance;
        private readonly PositiveFloat _maxDistance;

        public PositiveFloat Evaluate(PositiveFloatOrZero distance)
        {
            float alpha = math.saturate(math.unlerp(_minDistance, _maxDistance, distance));
            return math.lerp(_nearAmount, _farAmount, alpha);
        }
    }
}
