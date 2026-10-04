using UnityEngine;

namespace Tanks.Shared.Weapons.Data
{
    public interface ICooldownConfig
    {
        float DurationSeconds { get; }

        ICooldown Create();
    }

    [CreateAssetMenu(fileName = "Cooldown", menuName = "Scriptable Objects/Weapons/Cooldown")]
    public sealed class CooldownConfig : ScriptableObject, ICooldownConfig
    {
        [SerializeField]
        [Range(0.01f, 5)]
        private float _duration = 0.15f;

        public float DurationSeconds => _duration;

        public ICooldown Create() => new Cooldown(_duration);
    }
}
