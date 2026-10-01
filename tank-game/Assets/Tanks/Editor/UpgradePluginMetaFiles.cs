using UnityEditor;
using System.Linq;

namespace Tanks.Editor
{
    public static class UpgradePluginMetaFiles
    {
        [MenuItem("Tools/Plugins/Upgrade Plugin Metadata")]
        public static void UpgradeMetadata()
        {
            string[] guids = AssetDatabase.FindAssets("", new string[] { "Assets/Plugins" });
            string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            AssetDatabase.ForceReserializeAssets(
                paths, ForceReserializeAssetsOptions.ReserializeMetadata);

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Plugins/Upgrade Plugin Assets")]
        public static void UpgradeAssets()
        {
            string[] guids = AssetDatabase.FindAssets("", new string[] { "Assets/Plugins" });
            string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            AssetDatabase.ForceReserializeAssets(
                paths, ForceReserializeAssetsOptions.ReserializeAssets);

            AssetDatabase.Refresh();
        }
    }
}
