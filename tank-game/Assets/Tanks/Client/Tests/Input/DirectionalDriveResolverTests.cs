using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Input.Driving;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Input
{
    public sealed class DirectionalDriveResolverTests
    {
        private readonly DirectionalDriveResolverService _resolver = new();

        [TestCase(1, 0, 0)]
        [TestCase(0, 1, 90)]
        [TestCase(-1, 0, 180)]
        [TestCase(0, -1, -90)]
        [TestCase(1, 1, 45)]
        public void AlignedDirectionDrivesWithoutSteering(
            float x,
            float y,
            float heading)
        {
            ManualDriveInput result = _resolver.Resolve(
                new float2(x, y), heading, false, false, 0.02f, 80);

            Assert.That(result.Throttle, Is.EqualTo(1));
            Assert.That(result.Steering, Is.Zero.Within(0.0001f));
        }

        [TestCase(350, 10, 70, 0.1f)]
        [TestCase(10, 350, 120, 0.2f)]
        [TestCase(1, 0, 60, 0.1f)]
        [TestCase(-179, 179, 100, 0.1f)]
        public void SteeringUsesShortestArcAndDoesNotOvershoot(
            float heading,
            float target,
            float rate,
            float deltaTime)
        {
            float2 direction = new(math.cos(math.radians(target)),
                math.sin(math.radians(target)));
            float error = ((target - heading + 180) % 360 + 360) % 360 - 180;

            ManualDriveInput result = _resolver.Resolve(
                direction, heading, false, false, deltaTime, rate);

            Assert.That(math.abs(result.Steering), Is.LessThanOrEqualTo(1));
            Assert.That(-result.Steering * rate * deltaTime,
                Is.EqualTo(math.clamp(error, -rate * deltaTime, rate * deltaTime))
                    .Within(0.0001f));
        }

        [TestCase(0, false, true, -1, 0)]
        [TestCase(0.25f, false, false, 0.25f, 0)]
        [TestCase(0.6f, false, true, -1, 0)]
        [TestCase(0.6f, true, true, -1, 0)]
        [TestCase(0.6f, true, false, 0, 0)]
        public void ReverseAndHandbrakeUseCorrectHeadingAndMagnitude(
            float magnitude,
            bool turnInPlace,
            bool reverse,
            float throttle,
            float steering)
        {
            ManualDriveInput result = _resolver.Resolve(
                new float2(magnitude, 0), 0, turnInPlace, reverse, 0.1f, 90);

            Assert.That(result.Throttle, Is.EqualTo(throttle));
            Assert.That(result.Steering, Is.EqualTo(steering));
        }

        [TestCase(1, 0, 180)]
        [TestCase(-1, 0, 0)]
        [TestCase(0, -1, 90)]
        public void ExactHalfTurnAlwaysTurnsClockwise(
            float x,
            float y,
            float heading)
        {
            ManualDriveInput result = _resolver.Resolve(
                new float2(x, y), heading, false, false, 0.1f, 90);

            Assert.That(result.Steering, Is.EqualTo(1));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        public void NeutralInputNeverMovesOrTurns(bool turnInPlace, bool reverse)
        {
            ManualDriveInput result = _resolver.Resolve(
                default, 120, turnInPlace, reverse, 0.1f, 90);

            Assert.That(result, Is.EqualTo(default(ManualDriveInput)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveDeltaTimeDoesNotDivideOrTurn(float deltaTime)
        {
            ManualDriveInput result = _resolver.Resolve(
                new float2(0, 1), 0, false, false, deltaTime, 90);

            Assert.That(result.Steering, Is.Zero);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        [TestCase(240)]
        public void VariableFrameStepsConvergeAndStaySettled(
            int frameRate)
        {
            float heading = 350;
            float target = 10;
            float rate = 73;
            float2 direction = new(
                math.cos(math.radians(10)), math.sin(math.radians(10)));

            for (int frame = 0; frame < frameRate * 10; frame++)
            {
                float deltaTime = (frame % 2 == 0 ? 0.5f : 1.5f) / frameRate;
                float error = DeltaAngle(target, heading);

                ManualDriveInput input = _resolver.Resolve(
                    direction, heading, false, false, deltaTime, rate);

                float turn = -input.Steering * rate * deltaTime;

                Assert.That(math.abs(turn), Is.LessThanOrEqualTo(
                    math.abs(error) + 0.0001f));

                heading += turn;
            }

            ManualDriveInput settled = _resolver.Resolve(
                direction, heading, false, false, 1f / frameRate, rate);

            ManualDriveInput closer = _resolver.Resolve(
                direction, heading + DeltaAngle(target, heading) / 2,
                false, false, 1f / frameRate, rate);

            Assert.That(settled.Steering, Is.Zero);
            Assert.That(closer.Steering, Is.Zero);

            Assert.That(settled.Throttle,
                Is.EqualTo(1).Within(0.0001f));
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void FinalCorrectionIsGentlerAndIndependentOfFrameRate(float sign)
        {
            float2 direction = new(1, 0);

            ManualDriveInput near = _resolver.Resolve(
                direction, sign, true, false, 1f / 60, 90);

            ManualDriveInput farther = _resolver.Resolve(
                direction, sign * 2, true, false, 1f / 60, 90);

            ManualDriveInput faster = _resolver.Resolve(
                direction, sign, true, false, 1f / 240, 90);

            Assert.That(math.sign(near.Steering), Is.EqualTo(sign));

            Assert.That(math.abs(near.Steering), Is.LessThan(
                math.abs(farther.Steering)));

            Assert.That(faster.Steering, Is.EqualTo(near.Steering));
            Assert.That(near.Throttle, Is.Zero);
        }

        private static float DeltaAngle(float target, float heading)
        {
            float difference = math.radians(target - heading);

            return math.degrees(math.atan2(
                math.sin(difference), math.cos(difference)));
        }
    }
}
