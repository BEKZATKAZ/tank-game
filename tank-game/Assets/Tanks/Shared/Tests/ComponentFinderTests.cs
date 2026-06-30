using NUnit.Framework;
using System;
using Tanks.Shared.Adapters;

namespace Tanks.Shared.Tests
{
    public class CarBehaviour : INetworkBehaviourExtended
    {
        public ITransform Transform { get; }
    }

    public class PersonBehaviour : INetworkBehaviourExtended
    {
        public ITransform Transform { get; }
    }

    public class EngineComponent : INetworkComponent<CarBehaviour>
    {
        public CarBehaviour Behaviour { get; private set; }

        public void OnTick(int tick, float tickDelta) { }
    }

    public class TransmissionComponent : INetworkComponent<CarBehaviour>
    {
        public CarBehaviour Behaviour { get; private set; }

        public void OnTick(int tick, float tickDelta) { }
    }

    public class LifeStyleComponent : INetworkComponent<PersonBehaviour>
    {
        public PersonBehaviour Behaviour { get; private set; }

        public void OnTick(int tick, float tickDelta) { }
    }

    public class ComponentFinderTests
    {
        private INetworkBehaviourExtended _carBehaviour;
        private INetworkBehaviourExtended _personBehaviour;

        [SetUp]
        public void Setup()
        {
            _carBehaviour = new CarBehaviour();
            _personBehaviour = new PersonBehaviour();
        }

        [Test]
        public void FindFor_LooksForTargetType_ReturnsTypeArray()
        {
            Type abstractionType = typeof(INetworkComponent<>);

            Type[] carActual = SplitComponentFinder.FindFor(
                _carBehaviour, abstractionType, out bool cache);

            Type[] carExpected = { typeof(TransmissionComponent), typeof(EngineComponent), };

            ContainsTypes(carExpected, carActual);
            Assert.False(cache, "Unexisting data cannot be cached");

            Type[] personActual = SplitComponentFinder.FindFor(
                _personBehaviour, abstractionType, out cache);

            Type[] personExpected = { typeof(LifeStyleComponent) };

            ContainsTypes(personExpected, personActual);
            Assert.False(cache, "Unexisting data cannot be cached");

            // In case caching breaks
            carActual = SplitComponentFinder.FindFor(_carBehaviour, abstractionType, out cache);
            Assert.True(cache, "Reflection should not be called twice");
            ContainsTypes(carExpected, carActual);

            personActual = SplitComponentFinder.FindFor(
                _personBehaviour, abstractionType, out cache);

            Assert.True(cache, "Reflection should not be called twice");
            ContainsTypes(personExpected, personActual);
        }

        private void ContainsTypes(Type[] expected, Type[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            foreach (Type expectedType in expected)
            {
                Assert.Contains(expectedType, actual);
            }
        }
    }
}
