using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.UI.TankModel;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticPreviewService
    {
        public CosmeticPreviewService(
            CosmeticDatabase database,
            ITankModelService tankModels,
            ILocalPlayerService player)
        {
            _database = database;
            _tankModels = tankModels;
            _player = player;
            _equipped = player.LocalPlayer.Equipped.Value;
        }

        private readonly CosmeticDatabase _database;
        private readonly ITankModelService _tankModels;
        private readonly ILocalPlayerService _player;
        private EquippedCosmetics _equipped;
        private string _previewBody;
        private string _previewTurret;
        public bool Select(string identifier)
        {
            CosmeticConfig config = _database.Find(identifier, CosmeticType.Body)
                ?? _database.Find(identifier, CosmeticType.Turret);

            if (config == null) return false;

            if (config.CosmeticType == CosmeticType.Body) _previewBody = identifier;
            else _previewTurret = identifier;

            ApplyPreview();
            return true;
        }

        public EquippedCosmetics GetPreview()
        {
            CosmeticConfig body = _database.Find(_previewBody ?? _equipped.Body,
                CosmeticType.Body)
                ?? _database.Find("v3-body", CosmeticType.Body);

            CosmeticConfig turret = _database.Find(_previewTurret ?? _equipped.Turret,
                CosmeticType.Turret)
                ?? _database.Find("v3-turret", CosmeticType.Turret);

            return new EquippedCosmetics
            {
                Body = body ? body.Identifier : "v3-body",
                Turret = turret ? turret.Identifier : "v3-turret"
            };
        }

        private void ApplyPreview()
        {
            if (_previewBody == null && _previewTurret == null) return;
            _tankModels.PreviewCosmetics(0, GetPreview());
        }

        public void Clear()
        {
            _previewBody = null;
            _previewTurret = null;
            _tankModels.ClearCosmeticPreview(0);
        }

        public void Synchronize()
        {
            EquippedCosmetics equipped = _player.LocalPlayer.Equipped.Value;
            bool changed = equipped.Body != _equipped.Body || equipped.Turret !=
                _equipped.Turret;

            if (equipped.Body != _equipped.Body) _previewBody = null;
            if (equipped.Turret != _equipped.Turret) _previewTurret = null;
            _equipped = equipped;

            if (changed && _previewBody == null && _previewTurret == null)
            {
                _tankModels.ClearCosmeticPreview(0);
                return;
            }

            ApplyPreview();
        }
    }
}
