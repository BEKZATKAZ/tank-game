using System.IO;
using Tanks.Client.Items;
using Tanks.Shared.Items.Data;
using UnityEditor;
using UnityEngine;

namespace Tanks.Editor.Items
{
    public static class ItemGenerator
    {
        private static void WriteToAsset<T>(T asset, string path) where T : Object
        {
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (existingAsset == false)
            {
                AssetDatabase.CreateAsset(asset, path);
                return;
            }

            EditorUtility.CopySerialized(asset, existingAsset);
            existingAsset.name = Path.GetFileNameWithoutExtension(path);

            EditorUtility.SetDirty(existingAsset);
        }

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
                EditorItemConfig editorConfig = AssetDatabase
                    .LoadAssetByGUID<EditorItemConfig>(new GUID(guid));

                ItemConfig sharedConfig = new ItemConfig();
                ClientItemConfig clientConfig = new ClientItemConfig();

                sharedConfig.InitializeFromEditor(
                    editorConfig.Identifier,
                    editorConfig.Tier,
                    editorConfig.PlayerStats,
                    editorConfig.ItemStats);

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

                sharedDatabase.AddFromEditor(sharedConfig);
                clientDatabase.AddFromEditor(clientConfig);
            }

            WriteToAsset(sharedDatabase, sharedDatabasePath);
            WriteToAsset(clientDatabase, clientDatabasePath);

            AddressableUtility.MarkAddressable(
                sharedDatabasePath, "Gameplay-Shared", "SharedItemDatabase");

            AddressableUtility.MarkAddressable(
                clientDatabasePath, "Gameplay-Client", "ClientItemDatabase");

            AssetDatabase.SaveAssets();
        }
    }
}
