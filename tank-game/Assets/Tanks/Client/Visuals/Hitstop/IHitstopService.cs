using Cysharp.Threading.Tasks;
using System;
using Tanks.Client.Rendering;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Visuals.Hitstop
{
    public interface IHitstopService
    {
        UniTask RunAsync(float2 point, float durationSeconds);
    }

    public sealed class HitstopService : IHitstopService
    {
        public HitstopService(ICameraMainService cameraMain)
        {
            _cameraMain = cameraMain;
        }

        private readonly ICameraMainService _cameraMain;

        public async UniTask RunAsync(float2 point, float durationSeconds)
        {
            ICamera camera = _cameraMain.Get();
            if (camera is null) return;

            // Run only when the camera sees the source point
            float2 p = _cameraMain.Get().WorldToViewportPoint(point);
            if ((p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1) == false) return;

            Time.timeScale = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(durationSeconds), ignoreTimeScale: true);
            Time.timeScale = 1;
        }
    }
}
