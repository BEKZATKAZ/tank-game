using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Health
{
    public struct DamageTaken
    {
        public float Amount;
        public IDamageDealer From;
    }

    public struct HPHealed
    {
        public float Amount;
    }

    [RequireComponent(typeof(NetworkObject))]
    public class HealthBehaviour : NetworkBehaviourExtended, IDamageable, IHealable
    {
        [Range(10, 200)]
        [SerializeField]
        private float _maxHealth = 100;

        public NetworkVariable<float> Health { get; } = new(100);
        public float MaxHealth => _maxHealth;

        public void Damage(float amount, IDamageDealer from)
        {
            EmitMessage(new DamageTaken { Amount = amount, From = from });
        }

        public void Heal(float amount)
        {
            EmitMessage(new HPHealed { Amount = amount });
        }
    }
}