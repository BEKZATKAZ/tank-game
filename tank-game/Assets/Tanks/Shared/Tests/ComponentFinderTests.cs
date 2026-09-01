using NUnit.Framework;
using System;
using Tanks.Shared.Adapters;

namespace Tanks.Shared.Tests
{
    public interface ICarBehaviour : INetworkBehaviourExtended { }

    public class CarBehaviour : ICarBehaviour
    {
        public bool IsEnabled => true;
        public ITransform Transform { get; }
        public ulong OwnerClientId { get; }
        public bool IsOwner { get; }
        public bool IsLocalPlayer { get; }
        public bool IsServer { get; }
        public bool IsClient { get; }

        public T Require<T>() where T : INetworkComponent => default;
        public T GetBehaviour<T>() where T : INetworkBehaviourExtended => default;

        public void RegisterMessageHandler<T>(INetworkMessageHandler<T> handler) { }
        public void EmitMessage<T>(T message) { }
    }

    public class PersonBehaviour : INetworkBehaviourExtended
    {
        public bool IsEnabled => true;
        public ITransform Transform { get; }
        public ulong OwnerClientId { get; }
        public bool IsOwner { get; }
        public bool IsLocalPlayer { get; }
        public bool IsServer { get; }
        public bool IsClient { get; }

        public T Require<T>() where T : INetworkComponent => default;
        public T GetBehaviour<T>() where T : INetworkBehaviourExtended => default;

        public void RegisterMessageHandler<T>(INetworkMessageHandler<T> handler) { }
        public void EmitMessage<T>(T message) { }
    }

    public class EngineComponent : NetworkComponent<ICarBehaviour> { }
    public class TransmissionComponent : NetworkComponent<CarBehaviour> { }
    public class LifeStyleComponent : NetworkComponent<PersonBehaviour> { }
    public class AgeComponent : NetworkComponent<PersonBehaviour> { }

    public class ComponentFinderTests
    {
        private INetworkBehaviourExtended _carBehaviour;
        private INetworkBehaviourExtended _personBehaviour;

        [SetUp]
        public void Setup()
        {
            _carBehaviour = new CarBehaviour();
            _personBehaviour = new PersonBehaviour();
            SplitComponentFinder.ClearCache();
        }

        [Test]
        public void FindFor_LooksForTargetType_ReturnsTypeArray()
        {
            Type abstractionType = typeof(INetworkComponent<>);

            Type[] carActual = SplitComponentFinder.FindFor(
                _carBehaviour, abstractionType, isServer: false, out bool cache);

            Type[] carExpected = { typeof(TransmissionComponent), typeof(EngineComponent) };

            ContainsTypes(carExpected, carActual);
            Assert.False(cache, "Unexisting data cannot be cached");

            Type[] personActual = SplitComponentFinder.FindFor(
                _personBehaviour, abstractionType, out cache);

            Type[] personExpected = { typeof(LifeStyleComponent), typeof(AgeComponent) };

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
