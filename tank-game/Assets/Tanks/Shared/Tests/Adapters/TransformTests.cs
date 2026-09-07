using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Adapters
{
    public class TransformTests
    {
        private ITransform _adapter;
        private Transform _component;

        [SetUp]
        public void Setup()
        {
            _component = new GameObject("Transform Tests").transform;

            _adapter = new UnityTransform(_component)
            {
                WorldPosition = float2.zero,
                WorldRotation = Angle.FromDegrees(0),
                LocalScale = new float2(1, 1)
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_component == false) return;
            Object.DestroyImmediate(_component.gameObject);
        }

        [Test]
        public void Directions_AdaptsCorrectly()
        {
            AssertApprox(new float2(1, 0), _adapter.Right);
            AssertApprox(new float2(0, 1), _adapter.Up);

            _adapter.WorldRotation += Angle.FromDegrees(-90);

            AssertApprox(new float2(0, -1), _adapter.Right);
            AssertApprox(new float2(1, 0), _adapter.Up);
        }

        [Test]
        public void WorldSpace_AdaptsCorrectly()
        {
            AssertEquality(_component.position, _adapter.WorldPosition);
            AssertEquality(_component.rotation, _adapter.WorldRotation);
            AssertEquality(_component.lossyScale, _adapter.WorldScale);

            _adapter.WorldPosition = new float2(10, 1);
            _adapter.WorldRotation = Angle.FromDegrees(45);

            AssertEquality(new float2(10, 1), _component.position);
            AssertEquality(Angle.FromDegrees(45), _component.rotation);
            AssertEquality(new float2(1, 1), _component.lossyScale);

            AssertEquality(_adapter.WorldPosition, _component.position);
            AssertEquality(_adapter.WorldRotation, _component.rotation);
            AssertEquality(_adapter.WorldScale, _component.lossyScale);
        }

        [Test]
        public void LocalSpace_AdaptsCorrectly()
        {
            AssertEquality(_component.localPosition, _adapter.LocalPosition);
            AssertEquality(_component.localRotation, _adapter.LocalRotation);
            AssertEquality(_component.localScale, _adapter.LocalScale);

            _adapter.LocalPosition = new float2(5, 6);
            _adapter.LocalRotation = Angle.FromDegrees(45);
            _adapter.LocalScale *= 0.5f;

            AssertEquality(new float2(5, 6), _component.localPosition);
            AssertEquality(new float2(5, 6), _component.position);

            AssertEquality(Angle.FromDegrees(45), _component.localRotation);
            AssertEquality(Angle.FromDegrees(45), _component.rotation);

            AssertEquality(0.5f, _component.localScale);
            AssertEquality(0.5f, _component.lossyScale);

            AssertEquality(_adapter.LocalPosition, _component.localPosition);
            AssertEquality(_adapter.LocalRotation, _component.localRotation);
            AssertEquality(_adapter.LocalScale, _component.localScale);
        }

        [Test]
        public void SetParent_UnityTransform_AssignsParent()
        {
            Transform other = new GameObject().transform;
            ITransform otherAdapter = new UnityTransform(other);

            try
            {
                _adapter.SetParent(otherAdapter, true);
                Assert.AreEqual(other, _component.parent);

                _adapter.SetParent(otherAdapter, false);
                Assert.AreEqual(other, _component.parent);

                _adapter.SetParent(null, false);
                Assert.Null(_component.parent);
            }
            finally
            {
                Object.DestroyImmediate(other.gameObject);
            }
        }

        [Test]
        public void SetParent_UnknownType_Ignores()
        {
            ITransform unknown = Substitute.For<ITransform>();
            _adapter.SetParent(unknown, true);
            _adapter.SetParent(unknown, false);
        }

        [Test]
        public void DestroyedObject_ReturnsIsValidFalse()
        {
            Assert.True(_adapter.IsValid, "Should be true before destroying");
            Object.DestroyImmediate(_component.gameObject);
            Assert.False(_adapter.IsValid, "Should be false after destroying");
        }

        [Test]
        public void StandardMethods()
        {
            Assert.AreEqual(
                _component.GetHashCode(), _adapter.GetHashCode(),
                "Invalid GetHashCode()");

            Assert.AreEqual(
                _component.ToString(), _adapter.ToString(),
                "Invalid ToString()");

            Assert.True(_adapter.Equals(_adapter), "Invalid Equals()");
        }

        private static void AssertApprox(float2 expected, float2 actual)
        {
            Assert.True(math.distancesq(expected, actual) < (math.EPSILON * math.EPSILON),
                $"Expected: {expected}\n" +
                $"But was: {actual}");
        }

        private static void AssertEquality(float2 expected, Vector3 actual)
        {
            Assert.AreEqual(expected, ((float3)actual).xy);
        }

        private static void AssertEquality(Vector3 expected, float2 actual)
        {
            Assert.AreEqual(((float3)expected).xy, actual);
        }

        private static void AssertEquality(Angle expected, Quaternion actual)
        {
            Assert.True(Mathf.Approximately(
                expected.Degrees,
                Angle.FromDegrees(actual.eulerAngles.z).Degrees));
        }

        private static void AssertEquality(Quaternion expected, Angle actual)
        {
            Assert.True(Mathf.Approximately(
                Angle.FromDegrees(expected.eulerAngles.z).Degrees,
                actual.Degrees));
        }
    }
}
