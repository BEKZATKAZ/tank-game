using System.Runtime.InteropServices;
using Tanks.Core.Helpers;
using Unity.Mathematics;

namespace Tanks.Client.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct CameraImpulse
    {
        public CameraImpulse(
            float2 position,
            PositiveFloat amplitude,
            PositiveFloat frequency,
            PositiveFloat radius,
            PositiveFloat duration)
        {
            Position = position;
            Amplitude = amplitude;
            Frequency = frequency;
            Radius = radius;
            Duration = duration;
        }

        public readonly float2 Position;
        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Radius;
        public readonly float Duration;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ActiveCameraImpulse
    {
        public ActiveCameraImpulse(in CameraImpulse impulse, float startTime)
        {
            Impulse = impulse;
            StartTime = startTime;
        }

        public readonly CameraImpulse Impulse;
        public readonly float StartTime;
    }
}
