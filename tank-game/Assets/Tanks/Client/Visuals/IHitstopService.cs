using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    public interface IHitstopService
    {
        UniTask RunAsync(float durationSeconds);
    }

    public sealed class HitstopService : IHitstopService
    {
        public async UniTask RunAsync(float durationSeconds)
        {
            Time.timeScale = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(durationSeconds), ignoreTimeScale: true);
            Time.timeScale = 1;
        }
    }
}
