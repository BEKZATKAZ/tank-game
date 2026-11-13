using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Input.Bindings;
using Tanks.Client.Input.Glyphs;
using Tanks.Client.Input;
using UnityEditor;
using UnityEngine;

namespace Tanks.Client.Tests.Input
{
    public sealed class InputGlyphTests
    {
        [TestCase("Keyboard & Mouse/sample", "Keyboard & Mouse/sample")]
        [TestCase("Gamepad/sample_outline", "Gamepad/sample_outline")]
        [TestCase("Gamepad/sample_color", "Gamepad/sample_color")]
        public void KeysPreserveFoldersAndVariants(string relative, string expected)
        {
            string key = InputGlyphResolver.KeyFromAssetPath(
                "Assets/Tanks/Art/InputGlyphs/" + relative + ".png");

            Assert.That(key, Is.EqualTo(expected));
        }

        [Test]
        public void SerializedDictionaryAndSpriteReferencesSurviveReload()
        {
            const string PATH = "Assets/InputGlyphRoundTrip.asset";
            string spritePath = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets(
                "t:Sprite", new[] { "Assets/Tanks/Art/InputGlyphs" }).First());
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(spritePath)
                .OfType<Sprite>().First();
            InputGlyphDatabase database = ScriptableObject
                .CreateInstance<InputGlyphDatabase>();
            string key = InputGlyphResolver.KeyFromAssetPath(spritePath);

            try
            {
                database.SetGlyphs(
                    new Dictionary<string, Sprite> { { key, sprite } });
                AssetDatabase.CreateAsset(database, PATH);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(database);
                AssetDatabase.ImportAsset(PATH, ImportAssetOptions.ForceUpdate);
                InputGlyphDatabase loaded = AssetDatabase
                    .LoadAssetAtPath<InputGlyphDatabase>(PATH);

                Assert.That(loaded.TryGet(key, out Sprite actual), Is.True);
                Assert.That(actual, Is.EqualTo(sprite));

            }
            finally
            {
                AssetDatabase.DeleteAsset(PATH);
            }
        }

        [Test]
        public void DefaultBindingsResolveAndUnknownPathsHaveFallback()
        {
            InputGlyphDatabase database = Resources
                .Load<InputGlyphDatabase>("InputGlyphDatabase");
            Controls controls = new();

            try
            {
                Assert.That(database, Is.Not.Null);
                foreach (string id in ControlBindingSlots.BindingIds.Values)
                {
                    string path = controls.asset.bindings.First(x =>
                        x.id.ToString() == id).path;
                    if (string.IsNullOrEmpty(path)) continue;

                    Assert.That(InputGlyphResolver.Resolve(database, path),
                        Is.Not.Null, path);
                }

                Assert.That(InputGlyphResolver.Resolve(database, "<Unknown>/other"),
                    Is.Null);
            }
            finally
            {
                InputAssetLifetime.Destroy(controls.asset);
            }
        }
    }
}
