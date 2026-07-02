using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons.Projectiles
{
    public interface IProjectilePoolService
    {
        void LaunchAt(float2 worldPosition, Angle worldRotation, LaunchSettings settings);
        void Release(IProjectile instance);
    }
}