using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Adapters
{
    public interface IVelocity
    {
        float2 Linear { get; set; }
        Angle Angular { get; set; }

        void AddForce(float2 force, float2? position = null, bool impulse = false);
        void AddTorque(Angle torque, bool impulse = false);
    }

    public sealed class UnityVelocity : IVelocity
    {
        public UnityVelocity(Rigidbody2D rigidbody) => Rigidbody = rigidbody;

        public Rigidbody2D Rigidbody { get; }

        public float2 Linear
        {
            get => Rigidbody.linearVelocity;
            set => Rigidbody.linearVelocity = value;
        }

        public Angle Angular
        {
            get => Angle.FromDegrees(Rigidbody.angularVelocity);
            set => Rigidbody.angularVelocity = value.Degrees;
        }

        public void AddForce(float2 force, float2? position = null, bool impulse = false)
        {
            ForceMode2D mode = impulse ? ForceMode2D.Impulse : ForceMode2D.Force;

            if (position.HasValue == false)
            {
                Rigidbody.AddForce(force, mode);
                return;
            }

            Rigidbody.AddForceAtPosition(force, position.Value, mode);
        }

        public void AddTorque(Angle torque, bool impulse = false)
        {
            ForceMode2D mode = impulse ? ForceMode2D.Impulse : ForceMode2D.Force;
            Rigidbody.AddTorque(torque.Degrees, mode);
        }

        public override int GetHashCode() => Rigidbody.GetHashCode();
        public override string ToString() => Rigidbody.ToString();
        public override bool Equals(object obj)
        {
            return obj is UnityRigidbody other && Rigidbody.Equals(other.Rigidbody);
        }
    }
}
