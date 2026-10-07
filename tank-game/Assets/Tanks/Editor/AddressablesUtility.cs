using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Tanks.Editor
{
    public static class AddressableUtility
    {
        public static void MarkAddressable(string assetPath, string groupName, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == false)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"Could not find GUID for {assetPath}.");
                return;
            }

            AddressableAssetGroup group = settings.GetOrCreateGroup(groupName);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            entry.address = address;
            EditorUtility.SetDirty(settings);
        }

        private static AddressableAssetGroup GetOrCreateGroup(
            this AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);

            if (group) return group;

            return settings.CreateGroup(
                groupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null
            );
        }
    }
}
