using NUnit.Framework;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Tests.Items
{
    public abstract class ItemBehaviourTestBase<T> where T : ItemBehaviourBase, new()
    {
        public T Behaviour { get; private set; }
        public IItemOwner Owner { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            Owner = new MockItemOwner();
            Behaviour = new();
            Behaviour.Initialize(InitializeConfig());
        }

        protected abstract IItemConfig InitializeConfig();
    }
}
