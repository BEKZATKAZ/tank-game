using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Input.Glyphs;
using UnityEditor;
using UnityEngine;

namespace Tanks.Client.Editor
{
    public static class InputGlyphDatabaseBuilder
    {
        [MenuItem("Tanks/Input/Rebuild Glyph Database")]
        public static void Rebuild()
        {
            const string PATH =
                "Assets/Tanks/Client/Input/Resources/InputGlyphDatabase.asset";
            InputGlyphDatabase database = AssetDatabase
                .LoadAssetAtPath<InputGlyphDatabase>(PATH);

            if (database == null)
            {
                System.IO.Directory.CreateDirectory(
                    "Assets/Tanks/Client/Input/Resources");
                database = ScriptableObject.CreateInstance<InputGlyphDatabase>();
                AssetDatabase.CreateAsset(database, PATH);
            }

            Dictionary<string, Sprite> glyphs = new();
            string[] assets = AssetDatabase.FindAssets(
                "t:Sprite", new[] { "Assets/Tanks/Art/InputGlyphs" });

            foreach (string guid in assets.OrderBy(x => x))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string key = InputGlyphResolver.KeyFromAssetPath(path);
                Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>().FirstOrDefault();

                if (sprite != null) glyphs.Add(key, sprite);
            }

            database.SetGlyphs(glyphs);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
}
