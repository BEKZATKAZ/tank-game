using System.IO;
using Tanks.Client.Items;
using Tanks.Shared.Items.Data;
using UnityEditor;
using UnityEngine;

namespace Tanks.Editor.Items
{
    public static class ItemGenerator
    {
        [MenuItem("Tools/Game/Generate Item Files")]
        public static void Generate()
        {
            string[] guids = AssetDatabase.FindAssets("t:EditorItemConfig");

            if (guids.Length == 0)
            {
                Debug.Log("No item configs found");
                return;
            }

            string itemsPath = "Assets/Tanks/ScriptableObjects/Items/Runtime";
            string sharedDatabasePath = Path.Combine(itemsPath, "SharedItemDatabase.asset");
            string clientDatabasePath = Path.Combine(itemsPath, "ClientItemDatabase.asset");

            if (AssetDatabase.IsValidFolder(itemsPath) == false)
            {
                string parent = Path.GetDirectoryName(itemsPath);
                AssetDatabase.CreateFolder(parent, "Runtime");
            }

            var sharedDatabase = ScriptableObject.CreateInstance<SharedItemDatabaseConfig>();
            var clientDatabase = ScriptableObject.CreateInstance<ClientItemDatabaseConfig>();

            foreach (string guid in guids)
            {
                var editorConfig = AssetDatabase.LoadAssetByGUID<EditorItemConfig>(new GUID(guid));
                var sharedConfig = ScriptableObject.CreateInstance<ItemConfig>();
                var clientConfig = ScriptableObject.CreateInstance<ClientItemConfig>();

                sharedConfig.InitializeFromEditor(
                    editorConfig.Identifier,
                    editorConfig.Tier,
                    editorConfig.Stats);

                sharedConfig.InitializeActionsFromEditor(
                    editorConfig.OnPickUpActions);

                clientConfig.InitializeFromEditor(
                    editorConfig.Identifier);

                clientConfig.InitializeVisualsFromEditor(
                    editorConfig.IconMaterial,
                    editorConfig.Icon,
                    editorConfig.AccentColor);

                clientConfig.InitializeAudioFromEditor(
                    editorConfig.OnSpawnSound.Path,
                    editorConfig.OnPickUpSound.Path);

                string sharedConfigName = $"{editorConfig.Identifier}.asset";
                string clientConfigName = $"{editorConfig.Identifier} (Client).asset";

                string sharedConfigPath = Path.Combine(itemsPath, sharedConfigName);
                string clientConfigPath = Path.Combine(itemsPath, clientConfigName);

                AssetDatabase.CreateAsset(sharedConfig, sharedConfigPath);
                AssetDatabase.CreateAsset(clientConfig, clientConfigPath);

                sharedDatabase.AddFromEditor(sharedConfig);
                clientDatabase.AddFromEditor(clientConfig);
            }

            AssetDatabase.CreateAsset(sharedDatabase, sharedDatabasePath);
            AssetDatabase.CreateAsset(clientDatabase, clientDatabasePath);

            AddressablesUtility.MarkAddressable(
                assetPath: sharedDatabasePath,
                groupName: "Local_Gameplay",
                address: $"Gameplay/ItemDatabase",
                saveAssets: false);

            AddressablesUtility.MarkAddressable(
                assetPath: clientDatabasePath,
                groupName: "Local_Gameplay_Client",
                address: $"Gameplay/Client/ItemDatabase",
                saveAssets: true);
        }
    }
}
