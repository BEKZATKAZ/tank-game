using UnityEngine;
using Tanks.Core.Players;

namespace Tanks.UI.Menus.TankModel
{
    public sealed class TankModelEntry
    {
        public TankModelEntry(Transform model)
        {
            Model = model;
            SpriteRenderers = model.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        }

        public Transform Model { get; }
        public string DisplayName { get; internal set; } = "";
        public int Level { get; internal set; }
        public EquippedCosmetics Equipped { get; internal set; }
        public bool IsReady { get; internal set; }
        internal SpriteRenderer[] SpriteRenderers { get; }
    }
}
