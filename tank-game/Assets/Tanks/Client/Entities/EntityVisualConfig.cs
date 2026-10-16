using Tanks.Client.Visuals;
using UnityEngine;

namespace Tanks.Client.Entities
{
    [CreateAssetMenu(fileName = "EntityVisuals", menuName = "Scriptable Objects/Entity Visuals")]
    public sealed class EntityVisualConfig : ScriptableObject
    {
        [Header("Hit")]
        [SerializeField] private VisualType _hitVisualType;
        [SerializeField] private string _hitSoundPath;

        [Header("Destroy")]
        [SerializeField] private VisualType _destroyVisualType;
        [SerializeField] private bool _allowDestroyPrediction = true;
        [SerializeField] private string _destroyBySelfSoundPath;
        [SerializeField] private string _destroyByOtherSoundPath;
        [SerializeField] private string _destroyByPlayerSoundPath;

        public VisualType HitVisualType => _hitVisualType;
        public string HitSoundPath => _hitSoundPath;

        public VisualType DestoryVisualType => _destroyVisualType;
        public string DestroyBySelfSoundPath => _destroyBySelfSoundPath;
        public string DestroyByOtherSoundPath => _destroyByOtherSoundPath;
        public string DestroyByPlayerSoundPath => _destroyByPlayerSoundPath;
        public bool AllowDestroyPrediction => _allowDestroyPrediction;
    }
}
