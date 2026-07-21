using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Tanks.Editor
{
    public sealed class AddressablesUtility
    {
        public static void MarkAddressable(
            string assetPath, string groupName, string address, bool saveAssets = true)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                AddressableAssetSettingsDefaultObject.Settings =
                    AddressableAssetSettings.Create(
                        "Assets/AddressableAssetsData",
                        "AddressableAssetSettings",
                        true,
                        true
                    );

                settings = AddressableAssetSettingsDefaultObject.Settings;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                entry,
                true
            );

            if (saveAssets) AssetDatabase.SaveAssets();
        }
    }
}