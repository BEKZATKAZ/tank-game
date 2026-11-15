using System;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Tanks.Infrastructure.Tests.Runtime.Helpers
{
    public sealed class ControlledAssetProvider : ResourceProviderBase
    {
        private ProvideHandle _handle;

        public int RequestCount { get; private set; }

        public override Type GetDefaultType(IResourceLocation location)
        {
            return typeof(TextAsset);
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            _handle = provideHandle;
            RequestCount++;
        }

        public void Complete(TextAsset asset, Exception error = null)
        {
            _handle.Complete(asset, error == null, error);
        }
    }
}
