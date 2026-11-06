using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items.Behaviours.OnPickup;
using Tanks.Core.Settings;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Client.Tests.Items
{
    public sealed class RetrofierTests : ItemBehaviourTestBase<RetrofierBehaviour>
    {
        private IFrameRateLimitService _frameRate;

        protected override RetrofierBehaviour CreateBehaviour()
        {
            _frameRate = Substitute.For<IFrameRateLimitService>();
            return new RetrofierBehaviour(_frameRate);
        }

        [Test]
        public void RemotePickup_DoesNotCapThisClient()
        {
            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id });
            Behaviour.OnPickup(MockOwner);

            _frameRate.DidNotReceiveWithAnyArgs().SetLimit(default, default);
        }

        [Test]
        public void LocalPickups_CapThenRestoreOnDeathAndCleanup()
        {
            MockOwner.IsLocalPlayer.Returns(true);
            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id });
            Behaviour.OnPickup(MockOwner);
            _frameRate.Received(1).SetLimit(MockOwner, 20);

            MockOwner.Items.Returns(new[] { id, id });
            Behaviour.OnPickup(MockOwner);
            _frameRate.Received(1).SetLimit(MockOwner, 15);

            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(10, 100), new HealthState(0, 100));
            Behaviour.OnDespawn(MockOwner);
            Behaviour.OnDestroy(MockOwner);

            _frameRate.Received(3).RemoveLimit(MockOwner);
        }
    }
}
