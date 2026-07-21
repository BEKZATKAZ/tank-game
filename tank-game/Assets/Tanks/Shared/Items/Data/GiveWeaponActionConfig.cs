using NaughtyAttributes;
using Tanks.Core.Helpers;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Items.Data
{
    [CreateAssetMenu(
        fileName = "GiveWeapon",
        menuName = "Scriptable Objects/Items/Actions/Give Weapon")]
    public sealed class GiveWeaponActionConfig : ItemActionConfigBase
    {
        [Expandable]
        [Required]
        [SerializeField]
        private WeaponConfig _weapon;

        public override IItemAction Create(IObjectResolver resolver)
        {
            IWeaponFactory factory = resolver.Resolve<IWeaponFactory>();
            return new GiveWeaponAction(factory.AsRequired(), _weapon);
        }
    }
}
