using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Items.Behaviours
{
    public abstract class ItemBehaviourBase
    {
        protected IItemConfig Config { get; private set; }

        public virtual void Initialize(IItemConfig config) => Config = config;
        public abstract void OnPickup(IItemOwnerBehaviour owner);
    }
}
