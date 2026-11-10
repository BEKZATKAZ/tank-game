using System.Collections.Generic;
using Tanks.Core.Players;
using Tanks.UI.Menus.TankModel;

namespace Tanks.UI.TankModel
{
    public interface ITankModelService
    {
        IReadOnlyList<TankModelEntry> Models { get; }
        bool IsDrivingInPlace { get; }

        void SetPlayerData(int tankIndex, PlayerDisplayData data);
        void PreviewCosmetics(int tankIndex, EquippedCosmetics cosmetics);
        void ClearCosmeticPreview(int tankIndex);
        void SetReady(int tankIndex, bool ready);
        void StartDrivingInPlace();
        void DriveByCurve(int tankIndex, int curveIndex, bool instant);
        void StopDrivingInPlace();
        void Return(int index);
    }
}
