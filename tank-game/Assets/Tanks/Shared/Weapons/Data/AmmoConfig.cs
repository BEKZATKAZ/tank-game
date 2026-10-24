using NaughtyAttributes;
using System;
using UnityEngine;

namespace Tanks.Shared.Weapons.Data
{
    public interface IAmmoConfig
    {
        AmmoType Type { get; }

        ushort DefaultAmount { get; }
        ushort MaxAmount { get; }

        float RegenerationDelay { get; }
        float RegenerationDuration { get; }

        IAmmo Create();
    }

    public enum AmmoType
    {
        Fixed,
        Regenerating,
        Infinite
    }

    [CreateAssetMenu(fileName = "Ammo", menuName = "Scriptable Objects/Weapons/Ammo")]
    public class AmmoConfig : ScriptableObject, IAmmoConfig
    {
        [SerializeField]
        private AmmoType _type;

        [ShowIf("IsMagazineBased")]
        [SerializeField]
        private ushort _defaultAmount = 10;

        [ShowIf("_type", AmmoType.Regenerating)]
        [SerializeField]
        private ushort _maxAmount = 10;

        [ShowIf("_type", AmmoType.Regenerating)]
        [Range(0, 5)]
        [SerializeField]
        private float _regenerationDelay = 2;

        [ShowIf("_type", AmmoType.Regenerating)]
        [Range(1, 10)]
        [SerializeField]
        private float _regenerationDuration = 5;

        public AmmoType Type => _type;
        public ushort DefaultAmount => _defaultAmount;
        public ushort MaxAmount => _maxAmount;
        public float RegenerationDelay => _regenerationDelay;
        public float RegenerationDuration => _regenerationDuration;

        public bool IsMagazineBased() => _type is AmmoType.Fixed or AmmoType.Regenerating;

        public IAmmo Create() => _type switch
        {
            AmmoType.Fixed => new FixedAmmo(_defaultAmount),
            AmmoType.Regenerating => new RegeneratingAmmo(this),
            AmmoType.Infinite => new InfiniteAmmo(),
            _ => throw new NotImplementedException()
        };
    }
}
