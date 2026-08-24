using NSubstitute;
using NUnit.Framework;

namespace Tanks.Shared.Tests
{
    public interface ITestNetworkBehaviour : INetworkBehaviourExtended { }

    public class TestNetworkComponent : NetworkComponent<ITestNetworkBehaviour>
    {
        public int InitializeCalledCount { get; private set; }
        public int OnEnableCalled { get; private set; }
        public int UpdateCalledCount { get; private set; }
        public int TickCalledCount { get; private set; }
        public int OnDisableCalled { get; private set; }
        public int DestroyCalledCount { get; private set; }

        protected override void OnInitialize() => InitializeCalledCount++;
        protected override void OnEnable() => OnEnableCalled++;
        protected override void OnUpdate(float deltaTime) => UpdateCalledCount++;
        protected override void OnTick(int tick, float tickDelta) => TickCalledCount++;
        protected override void OnDisable() => OnDisableCalled++;
        protected override void OnDestroy() => DestroyCalledCount++;
    }

    public sealed class NetworkComponentTests
    {
        [Test]
        public void Events_Initialize_InvokesCorrectly()
        {
            ITestNetworkBehaviour behaviour = Substitute.For<ITestNetworkBehaviour>();
            TestNetworkComponent component = new TestNetworkComponent();

            component.Initialize(behaviour);
            Assert.AreEqual(1, component.InitializeCalledCount);
            Assert.AreEqual(1, component.OnEnableCalled);
            Assert.AreEqual(0, component.DestroyCalledCount);

            component.IsEnabled = true;
            Assert.AreEqual(0, component.OnDisableCalled);
            Assert.AreEqual(1, component.OnEnableCalled);

            component.IsEnabled = false;
            Assert.AreEqual(1, component.OnDisableCalled);
            Assert.AreEqual(1, component.OnEnableCalled);

            component.IsEnabled = true;
            Assert.AreEqual(1, component.OnDisableCalled);
            Assert.AreEqual(2, component.OnEnableCalled);

            component.Destroy();
            Assert.AreEqual(2, component.OnDisableCalled);
            Assert.AreEqual(1, component.DestroyCalledCount);
        }

        [Test]
        public void Events_Updates_InvokesCorrectly()
        {
            ITestNetworkBehaviour behaviour = Substitute.For<ITestNetworkBehaviour>();
            TestNetworkComponent component = new TestNetworkComponent();

            component.Initialize(behaviour);
            component.Update(0.01f);
            component.Update(0.01f);
            component.Update(0.01f);

            Assert.AreEqual(3, component.UpdateCalledCount);

            component.Tick(1, 0.1f);
            component.Tick(2, 0.1f);

            Assert.AreEqual(2, component.TickCalledCount);
        }
    }
}
