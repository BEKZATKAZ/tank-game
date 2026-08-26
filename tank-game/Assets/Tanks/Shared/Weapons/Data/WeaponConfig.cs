using NaughtyAttributes;
using Tanks.Shared.Weapons.Data.Actions;
using UnityEngine;

namespace Tanks.Shared.Weapons.Data
{
    public enum WeaponUseMode
    {
        SemiAutomatic,
        FullAutomatic
    }

    public interface IWeaponConfig
    {
        WeaponUseMode UseMode { get; }
        IAmmoConfig Ammo { get; }
        ICooldownConfig Cooldown { get; }
        IUseActionConfig UseAction { get; }
        IRecoilConfig Recoil { get; }
    }

    [CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapons/Weapon")]
    public class WeaponConfig : ScriptableObject, IWeaponConfig
    {
        [SerializeField]
        private WeaponUseMode _useMode;

        [Expandable]
        [Required]
        [SerializeField]
        private AmmoConfig _ammo;

        [Expandable]
        [Required]
        [SerializeField]
        private CooldownConfig _cooldown;

        [Expandable]
        [Required]
        [SerializeField]
        private UseActionConfig _useAction;

        [SerializeField]
        private RecoilConfig _recoil;

        public WeaponUseMode UseMode => _useMode;
        public IAmmoConfig Ammo => _ammo;
        public ICooldownConfig Cooldown => _cooldown;
        public IUseActionConfig UseAction => _useAction;
        public IRecoilConfig Recoil => _recoil;
    }
}
