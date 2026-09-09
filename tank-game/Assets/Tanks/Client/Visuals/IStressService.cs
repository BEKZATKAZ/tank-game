using Tanks.Core.Helpers;
using Unity.Mathematics;

namespace Tanks.Client.Visuals
{
    public interface IStressService
    {
        float Stress { get; }

        float AddStress(PositiveFloat amount);
        float SetMinStress(PositiveFloat amount);
        void ResetStress();
    }

    public sealed class StressService : IStressService
    {
        public float Stress { get; private set; }

        public float AddStress(PositiveFloat amount)
        {
            return Stress = math.min(Stress + amount, 1);
        }

        public float SetMinStress(PositiveFloat amount)
        {
            return Stress = math.clamp(amount, Stress, 1);
        }

        public void ResetStress()
        {
            Stress = 0;
        }
    }
}
