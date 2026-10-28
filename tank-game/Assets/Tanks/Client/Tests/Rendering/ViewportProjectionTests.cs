using NUnit.Framework;
using Tanks.Client.Rendering;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Rendering
{
    public class ViewportProjectionTests
    {
        [Test]
        public void SlowCameraMovement_HoldsUntilTheRenderedCameraCrossesAPixel()
        {
            float2 world = new float2(2, 1);
            float2 initial = Project(world, new float3(0, 0, -10));
            float2 subpixel = Project(world, new float3(0.02f, 0, -10));
            float2 nextPixel = Project(world, new float3(0.04f, 0, -10));

            Assert.AreEqual(initial, subpixel);
            Assert.That(nextPixel.x - initial.x, Is.EqualTo(-1f / 640).Within(0.000001f));
            Assert.AreEqual(initial.y, nextPixel.y);
        }

        [Test]
        public void MovingTank_AdvancesInScenePixels()
        {
            float3 camera = new float3(0, 0, -10);
            float2 initial = Project(float2.zero, camera);
            float2 subpixel = Project(new float2(0.02f, 0), camera);
            float2 nextPixel = Project(new float2(0.04f, 0), camera);

            Assert.AreEqual(initial, subpixel);
            Assert.That(nextPixel.x - initial.x, Is.EqualTo(1f / 640).Within(0.000001f));
        }

        [Test]
        public void CameraAndTargetMoveTogether_AnchorRemainsAtTheSameScreenPosition()
        {
            float2 initial = Project(new float2(2, 1), new float3(0, 0, -10));
            float2 moved = Project(new float2(2.25f, 1.125f), new float3(0.25f, 0.125f, -10));

            Assert.AreEqual(initial, moved);
        }

        [Test]
        public void CameraShake_UsesCurrentRotationAndPosition()
        {
            var projection = new ViewportProjection(
                new float3(0.25f, 0.125f, -10), quaternion.RotateZ(math.PI * 0.5f),
                new float2(40, 22.5f), 1f / 16, false, new float2(640, 360));
            float2 viewport = projection.WorldToViewportPoint(new float2(0.25f, 1.125f));

            Assert.That(viewport.x, Is.EqualTo(0.525f).Within(0.000001f));
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.000001f));
        }

        [TestCase(640, 360)]
        [TestCase(800, 450)]
        [TestCase(640, 400)]
        public void ResolutionChange_PreservesWorldOffsetInScenePixels(int width, int height)
        {
            var projection = new ViewportProjection(
                new float3(0, 0, -10), quaternion.identity,
                new float2(width, height) / 16, 1f / 16, false, new float2(width, height));
            float2 center = projection.WorldToViewportPoint(new float2(1, 1));
            float2 anchor = projection.WorldToViewportPoint(new float2(1, 1.5f));

            Assert.AreEqual(center.x, anchor.x);
            Assert.That((anchor.y - center.y) * height, Is.EqualTo(8).Within(0.0001f));
        }

        [Test]
        public void PixelSnapping_SnapsWorldPositionBeforeCameraRotation()
        {
            var projection = new ViewportProjection(
                new float3(0, 0, -10), quaternion.RotateZ(0.1f),
                new float2(40, 22.5f), 1f / 16, true, float2.zero);

            Assert.AreEqual(
                projection.WorldToViewportPoint(float2.zero),
                projection.WorldToViewportPoint(new float2(0.02f)));
        }

        [Test]
        public void WithoutPixelPerfectCamera_ProjectsSmoothlyFromCurrentPose()
        {
            var projection = new ViewportProjection(
                new float3(0.02f, 0, -10), quaternion.identity,
                new float2(40, 22.5f), 0, false, float2.zero);
            float2 viewport = projection.WorldToViewportPoint(float2.zero);

            Assert.That(viewport.x, Is.EqualTo(0.4995f).Within(0.000001f));
            Assert.AreEqual(0.5f, viewport.y);
        }

        private static float2 Project(float2 world, float3 camera)
        {
            var projection = new ViewportProjection(
                camera, quaternion.identity, new float2(40, 22.5f),
                1f / 16, false, new float2(640, 360));
            return projection.WorldToViewportPoint(world);
        }
    }
}
