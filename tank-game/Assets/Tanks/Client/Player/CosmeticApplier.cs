using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using UnityEngine;

namespace Tanks.Client.Player
{
    public sealed class CosmeticApplier : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _bodySprite;
        [SerializeField] private SpriteRenderer _turretSprite;
        [SerializeField] private CosmeticDatabase _database;
        [SerializeField] private CosmeticConfig _defaultBody;
        [SerializeField] private CosmeticConfig _defaultTurret;

        public void Apply(EquippedCosmetics equipped)
        {
            Apply(_database ? _database.Find(equipped.Body, CosmeticType.Body) : null,
                _database ? _database.Find(equipped.Turret, CosmeticType.Turret) : null);
        }

        public void Apply(CosmeticConfig body, CosmeticConfig turret)
        {
            body = body && body.CosmeticType == CosmeticType.Body ? body : _defaultBody;
            turret = turret && turret.CosmeticType == CosmeticType.Turret ? turret : _defaultTurret;
            if (_bodySprite && body) _bodySprite.sprite = body.Sprite;
            if (_turretSprite && turret) _turretSprite.sprite = turret.Sprite;
        }

        private void Awake() => Apply(default(EquippedCosmetics));
    }
}
