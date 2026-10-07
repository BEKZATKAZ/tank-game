using System;
using UnityEngine;

namespace Tanks.Infrastructure
{
    [Serializable]
    public sealed class EnvironmentSettings
    {
        [SerializeField] private string _environmentName;
        [SerializeField] private string _windowsBucketId;
        [SerializeField] private string _macOSBucketId;
        [SerializeField] private string _linuxBucketId;

        public string EnvironmentName => _environmentName;

        public string GetBucketId()
        {
#if UNITY_STANDALONE_WIN
            return _windowsBucketId;
#elif UNITY_STANDALONE_LINUX
            return _linuxBucketId;
#elif UNITY_STANDALONE_OSX
            return _macOSBucketId;
#else
            throw new System.NotSupportedException("This platform is not supported");
#endif
        }
    }
}
