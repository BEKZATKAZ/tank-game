using NUnit.Framework;
using System.IO;
using System.Linq;
using Tanks.Client.Player;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;

namespace Tanks.UI.Tests.Menus
{
    public sealed class CosmeticAssetsTests
    {
        private const string DatabasePath = "Assets/Tanks/ScriptableObjects/Cosmetics/CosmeticDatabase.asset";

        [Test]
        public void Database_CoversEveryPackSpriteWithUniqueIdentifiersAndCorrectSlots()
        {
            var database = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(DatabasePath);
            var sprites = Directory.GetFiles("Assets/Plugins/Layer Lab/2D Pixel-TankPack/Tank", "*.png");

            Assert.That(database.Cosmetics.Count, Is.EqualTo(110));
            Assert.That(database.Cosmetics.Select(x => x.Identifier).Distinct().Count(), Is.EqualTo(110));

            foreach (string path in sprites)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                bool body = name.EndsWith("_Bot");
                string id = name.Substring(0, name.Length - 4).ToLowerInvariant().Replace(' ', '-') +
                    (body ? "-body" : "-turret");
                CosmeticConfig config = database.Find(id, body ? CosmeticType.Body : CosmeticType.Turret);
                Assert.NotNull(config, id);
                Assert.That(AssetDatabase.GetAssetPath(config.Sprite), Is.EqualTo(path.Replace('\\', '/')), id);
            }

            Assert.NotNull(database.Find("v3-turret", CosmeticType.Turret));
            Assert.IsNull(database.Find("v3-turret", CosmeticType.Body));
        }

        [Test]
        public void CosmeticApplier_AppliesIndependentSlotsAndFallsBackForUnknownIds()
        {
            var database = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(DatabasePath);
            var tank = new GameObject("Cosmetic Test Tank");

            try
            {
                var body = tank.AddComponent<SpriteRenderer>();
                var turretObject = new GameObject("Turret");
                turretObject.transform.SetParent(tank.transform);
                var turret = turretObject.AddComponent<SpriteRenderer>();
                var applier = tank.AddComponent<CosmeticApplier>();
                var serialized = new SerializedObject(applier);
                serialized.FindProperty("_database").objectReferenceValue = database;
                serialized.FindProperty("_bodySprite").objectReferenceValue = body;
                serialized.FindProperty("_turretSprite").objectReferenceValue = turret;
                serialized.FindProperty("_defaultBody").objectReferenceValue = database.Find("v3-body", CosmeticType.Body);
                serialized.FindProperty("_defaultTurret").objectReferenceValue = database.Find("v3-turret", CosmeticType.Turret);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                applier.Apply(new EquippedCosmetics { Body = "v4-body", Turret = "kr1-black-turret" });
                Assert.That(body.sprite, Is.EqualTo(database.Find("v4-body", CosmeticType.Body).Sprite));
                Assert.That(turret.sprite, Is.EqualTo(database.Find("kr1-black-turret", CosmeticType.Turret).Sprite));

                applier.Apply(new EquippedCosmetics { Body = "v4-turret", Turret = "unknown" });
                Assert.That(body.sprite, Is.EqualTo(database.Find("v3-body", CosmeticType.Body).Sprite));
                Assert.That(turret.sprite, Is.EqualTo(database.Find("v3-turret", CosmeticType.Turret).Sprite));
            }
            finally { Object.DestroyImmediate(tank); }
        }

        [TestCase("en")]
        [TestCase("ru")]
        [TestCase("fr")]
        public void StoreLocalization_ContainsCurrencyTextAndSmartTimers(string locale)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>($"Assets/Tanks/Localization/Tables/Menus/Menus_{locale}.asset");
            Assert.That(table.GetEntry("menu.reward.timer").IsSmart, Is.True);
            Assert.That(table.GetEntry("menu.store.confirm-message").IsSmart, Is.True);

            foreach (string key in new[] { "currency.parts.name", "currency.parts.description", "currency.pride.name",
                "currency.pride.description", "menu.reward.claim", "menu.store.equip", "menu.store.load-error" })
            {
                Assert.That(table.GetEntry(key)?.LocalizedValue, Is.Not.Null.And.Not.Empty, key);
            }
        }

        [TestCase("MenuTank")]
        [TestCase("Player")]
        public void TankPrefabs_HaveDatabaseAndValidDefaults(string name)
        {
            var tank = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Tanks/Prefabs/{name}.prefab");
            var applier = tank.GetComponent<CosmeticApplier>();
            Assert.NotNull(applier);
            var serialized = new SerializedObject(applier);

            Assert.NotNull(serialized.FindProperty("_database").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("_bodySprite").objectReferenceValue);
            Assert.NotNull(serialized.FindProperty("_turretSprite").objectReferenceValue);
            var body = serialized.FindProperty("_defaultBody").objectReferenceValue as CosmeticConfig;
            var turret = serialized.FindProperty("_defaultTurret").objectReferenceValue as CosmeticConfig;
            Assert.That(body.Identifier, Is.EqualTo("v3-body"));
            Assert.That(turret.Identifier, Is.EqualTo("v3-turret"));
            Assert.That(turret.CosmeticType, Is.EqualTo(CosmeticType.Turret));
        }

        [Test]
        public void LocalizedVariables_ReplaceValuesOnRepeatedCountdownUpdates()
        {
            var original = new LocalizedString();
            var text = new UnityLocalizedString(original, "timer");
            text.AddVariable("seconds", 59);
            text.AddVariable("seconds", 58);

            Assert.That(((IntVariable)original["seconds"]).Value, Is.EqualTo(58));
        }
    }
}
