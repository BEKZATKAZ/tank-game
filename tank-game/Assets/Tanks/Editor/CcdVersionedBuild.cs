using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

namespace Tanks.Editor
{
    public static class CcdVersionedBuild
    {
        private const string MANAGED_BUILD_PATH =
            "CCDBuildData/ManagedEnvironment/ManagedBucket/ManagedBadge";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            // Initialize the built-in handlers before prepending our handler.
            _ = CcdBuildEvents.Instance;

            CcdBuildEvents.PrependPostBuildEvent(PrepareVersionedRelease);
            CcdBuildEvents.PrependPostUpdateEvent(PrepareVersionedRelease);
        }

        private static Task<bool> PrepareVersionedRelease(
            AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result)
        {
            if (string.IsNullOrEmpty(result.Error) == false)
            {
                return Task.FromResult(false);
            }

            string buildPath = input.AddressableSettings.profileSettings.GetValueById(
                input.AddressableSettings.activeProfileId,
                input.AddressableSettings.RemoteCatalogBuildPath.Id);

            if (buildPath != MANAGED_BUILD_PATH)
            {
                return Task.FromResult(true);
            }

            if (Directory.Exists(buildPath) == false)
            {
                Debug.LogError("CCD managed build output was not found.");
                return Task.FromResult(false);
            }

            string badge = $"release-{PlayerSettings.bundleVersion.Replace('.', '-')}";
            string versionedPath = Path.Combine(Path.GetDirectoryName(buildPath), badge);

            // CCD uses the folder name as the release badge during upload.
            Directory.Move(buildPath, versionedPath);

            return Task.FromResult(true);
        }
    }
}
