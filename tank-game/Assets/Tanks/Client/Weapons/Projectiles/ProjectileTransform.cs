using System;
using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Client.Weapons.Projectiles
{
    public class ProjectileTransform : ITransform
    {
        public ProjectileTransform(float2 worldScale) => WorldScale = worldScale;

        public bool IsValid => true;
        public float2 WorldPosition { get; set; }
        public Angle WorldRotation { get; set; }
        public float2 WorldScale { get; }

        // These are not used in projectile movement
        public float2 LocalPosition
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Angle LocalRotation
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public float2 LocalScale
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public void SetParent(ITransform parent, bool keepWorldPosition = true) { }
    }
}
