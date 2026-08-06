using NaughtyAttributes;
using Tanks.Shared.Weapons.Data;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons
{
    [RequireComponent(typeof(WeaponHolderBehaviour))]
    public sealed class DefaultWeaponInitializer : NetworkBehaviourExtended
    {
        [Required]
        [SerializeField]
        private WeaponConfig _defaultWeaponConfig;

        [Inject]
        private readonly IWeaponFactory _factory;

        protected override void OnInitialize()
        {
            WeaponHolderBehaviour behaviour = GetComponent<WeaponHolderBehaviour>();
            behaviour.SetDefault(_factory.Create(_defaultWeaponConfig, behaviour.MuzzlePoint));
            behaviour.SwitchToDefault();
        }
    }
}
