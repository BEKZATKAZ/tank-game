using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;

namespace Tanks.Shared.Health
{
    public interface IDamageDealer
    {
        ITransform Transform { get; }
    }

    public interface IDamageable
    {
        void Damage(PositiveFloat amount, IDamageDealer from);
    }

    public interface IHealable
    {
        void Heal(PositiveFloat amount);
    }
}
