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

        void StartLoading<T>(
            T parameters, bool withTips = true, bool loadActualScene = true, float loadUntil = 0.5f)
            where T : ISceneParameters;
    }
}
