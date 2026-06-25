using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.Shared.Adapters
{
    public readonly struct Angle
    {
        private Angle(float value) => _value = value;

        public float Radians => math.radians(_value);
        public float Degrees => math.degrees(_value);

        private readonly float _value;

        public static Angle FromDegrees(float degrees) => new(degrees);
        public static Angle FromRadians(float radians) => new(math.degrees(radians));
    }

    public interface ITransform
    {
        float2 WorldPosition { get; set; }
        Angle WorldRotation { get; set; }
        float2 WorldScale { get; }

        float2 LocalPosition { get; set; }
        Angle LocalRotation { get; set; }
        float2 LocalScale { get; set; }

        float2 Right
        {
            get
            {
                float angle = WorldRotation.Radians;
                return new float2(math.cos(angle), math.sin(angle));
            }
        }

        float2 Up => Right.yx * new float2(-1, 1);
    }

    public sealed class UnityTransform : ITransform
    {
        public UnityTransform(Transform transform)
        {
            Transform = transform;
        }

        public Transform Transform { get; }

        public float2 WorldPosition
        {
            get => ((float3)Transform.position).xy;
            set => Transform.position = (Vector2)value;
        }

        public float2 LocalPosition
        {
            get => ((float3)Transform.localPosition).xy;
            set => Transform.localPosition = (Vector2)value;
        }

        public Angle WorldRotation
        {
            get => Angle.FromDegrees(Transform.rotation.eulerAngles.z);
            set => Transform.rotation = Quaternion.Euler(0, 0, value.Degrees);
        }

        public Angle LocalRotation
        {
            get => Angle.FromDegrees(Transform.localRotation.eulerAngles.z);
            set => Transform.localRotation = Quaternion.Euler(0, 0, value.Degrees);
        }

        public float2 WorldScale
        {
            get => ((float3)Transform.lossyScale).xy;
        }

        public float2 LocalScale
        {
            get => ((float3)Transform.localScale).xy;
            set => Transform.localScale = (Vector2)value;
        }

        public override int GetHashCode() => Transform.GetHashCode();
        public override string ToString() => Transform.ToString();
        public override bool Equals(object obj)
        {
            return obj is UnityTransform other && Transform.Equals(other.Transform);
        }
    }
}
