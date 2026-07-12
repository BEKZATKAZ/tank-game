using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Adapters
{
    public sealed class MockTransform : ITransform
    {
        public bool IsValid { get; set; } = true;

        public float2 WorldPosition { get; set;}
        public Angle WorldRotation { get; set; }
        public float2 WorldScale { get; }

        public float2 LocalPosition { get; set; }
        public Angle LocalRotation { get; set; }
        public float2 LocalScale { get; set; }
    }

    public sealed class MockRigidbody : IRigidbody
    {
        public MockRigidbody(ITransform transform) => _transform = transform;

        private readonly ITransform _transform;

        public void Move(float2 delta)
        {
            _transform.WorldPosition += delta;
        }

        public void Rotate(Angle angle)
        {
            _transform.WorldRotation += angle;
        }
    }

    public sealed class MockVelocity : IVelocity
    {
        public float2 Linear { get; set; }
        public Angle Angular { get; set; }

        public void AddForce(float2 force, float2? position = null, bool impulse = false)
        {
            Linear += force;
        }

        public void AddTorque(Angle torque, bool impulse = false)
        {
            Angular += torque;
        }
    }
}
