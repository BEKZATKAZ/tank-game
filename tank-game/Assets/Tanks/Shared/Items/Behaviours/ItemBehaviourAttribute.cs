using System;

namespace Tanks.Shared.Items.Behaviours
{
    public sealed class ItemBehaviourAttribute : Attribute
    {
        public ItemBehaviourAttribute(string targetIdentifier)
        {
            TargetIdentifier = new ItemIdentifier(targetIdentifier);
        }

        public ItemIdentifier TargetIdentifier { get; }
    }
}
