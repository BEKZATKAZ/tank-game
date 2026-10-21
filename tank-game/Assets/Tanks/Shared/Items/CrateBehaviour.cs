using System.Collections.Generic;
using Tanks.Shared.Items.Data;
using UnityEngine;

namespace Tanks.Shared.Items
{
    public sealed class CrateBehaviour : NetworkBehaviourExtended, ICrateBehaviour
    {
        [SerializeField] private Dictionary<ItemTier, float> _chances = new();

        public IReadOnlyDictionary<ItemTier, float> Chances => _chances;
        public bool CanDropItems { get; set; } = true;
    }
}
