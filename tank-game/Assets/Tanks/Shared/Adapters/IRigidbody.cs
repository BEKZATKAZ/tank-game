using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Adapters
{
    public interface IRigidbody
    {
        void Move(float2 delta);
        void Rotate(Angle angle);
    }

    public sealed class UnityRigidbody : IRigidbody
    {
        public UnityRigidbody(Rigidbody2D rigidbody) => Rigidbody = rigidbody;

        public Rigidbody2D Rigidbody { get; }

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