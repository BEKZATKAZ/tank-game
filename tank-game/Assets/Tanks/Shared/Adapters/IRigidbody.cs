using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Adapters
{
    public interface IRigidbody
    {
        float2 LinearVelocity { get; set; }
        Angle AngularVelocity { get; set; }

        void Move(float2 delta);
        void Rotate(Angle angle);
    }

    public sealed class UnityRigidbody : IRigidbody
    {
        public UnityRigidbody(Rigidbody2D rigidbody) => Rigidbody = rigidbody;

        public Rigidbody2D Rigidbody { get; }

        public float2 LinearVelocity
        {
            get => Rigidbody.linearVelocity;
            set => Rigidbody.linearVelocity = value;
        }

        public Angle AngularVelocity
        {
            get => Angle.FromDegrees(Rigidbody.angularVelocity);
            set => Rigidbody.angularVelocity = value.Degrees;
        }

        public void Move(float2 delta)
        {
            Rigidbody.MovePosition(Rigidbody.position + (Vector2)delta);
        }

        public void Rotate(Angle angle)
        {
            Rigidbody.MoveRotation(angle.Degrees);
        }

        public override int GetHashCode() => Rigidbody.GetHashCode();
        public override string ToString() => Rigidbody.ToString();
        public override bool Equals(object obj)
        {
            return obj is UnityRigidbody other && Rigidbody.Equals(other.Rigidbody);
        }
    }
}