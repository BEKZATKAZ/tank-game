using Tanks.Core.Helpers;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Health
{
    public struct DamageTaken
    {
        public PositiveFloat Amount;
    }

    public struct HPHealed
    {
        public PositiveFloat Amount;
    }

    [RequireComponent(typeof(NetworkObject))]
    public class HealthBehaviour : NetworkBehaviourExtended, IDamageable, IHealable
    {
        [Range(10, 200)]
        [SerializeField]
        private float _maxHealth = 100;

        public NetworkVariable<float> Health { get; } = new(100);
        public PositiveFloatOrZero MaxHealth => _maxHealth;

        public void Damage(PositiveFloat amount)
        {
            EmitMessage(new DamageTaken { Amount = amount });
        }

        public void Heal(PositiveFloat amount)
        {
            EmitMessage(new HPHealed { Amount = amount });
        }
    }
}
