using Cysharp.Threading.Tasks;
using System;

namespace Tanks.Core.Common.Scenes
{
    public interface ISceneLoader
    {
        event Action LoadingStarted;
        event Action LoadingEnded;

        float Progress { get; set; }
        bool WithTips { get; }
        bool IsLoading { get; }

        T GetLoadingParameters<T>() where T : ISceneParameters;
        void EndLoading<T>() where T : ISceneParameters;

        UniTask StartLoadingAsync<T>(
            T parameters, bool withTips = true, bool loadActualScene = true)
            where T : ISceneParameters;
    }
}
