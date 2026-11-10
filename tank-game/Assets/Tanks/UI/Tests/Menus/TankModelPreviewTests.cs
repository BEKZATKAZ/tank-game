using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.UI.Menus.TankModel;
using UnityEditor;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public sealed class TankModelPreviewTests
    {
        private GameObject _root;
        private Transform _tank;
        private TankModelService _service;
        private CosmeticDatabase _database;
        private SpriteRenderer _body;
        private SpriteRenderer _turret;

        [SetUp]
        public void Setup()
        {
            _database = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(
                "Assets/Tanks/ScriptableObjects/Cosmetics/CosmeticDatabase.asset");

            _root = new GameObject("Tank Model Preview Test");
            _root.SetActive(false);
            _service = _root.AddComponent<TankModelService>();
            _tank = new GameObject("Tank").transform;
            _tank.SetParent(_root.transform);
            _body = _tank.gameObject.AddComponent<SpriteRenderer>();
            var turretObject = new GameObject("Turret");
            turretObject.transform.SetParent(_tank);
            _turret = turretObject.AddComponent<SpriteRenderer>();
            var applier = _tank.gameObject.AddComponent<CosmeticApplier>();
            Set(applier, "_database", _database);
            Set(applier, "_bodySprite", _body);
            Set(applier, "_turretSprite", _turret);
            Set(applier, "_defaultBody", _database.Find("v3-body", CosmeticType.Body));
            Set(applier, "_defaultTurret", _database.Find("v3-turret", CosmeticType.Turret));
            Set(_service, "_tankModels", new[] { _tank });
            Set(_service, "_mainCameraCenter", _root.transform);
            Set(_service, "_cameraFollow", Substitute.For<ICameraFollowService>());
            ((List<TankModelEntry>)_service.Models).Add(new TankModelEntry(_tank));
            _service.SetPlayerData(0, new PlayerDisplayData
            {
                DisplayName = "Player",
                Equipped = EquippedCosmetics.Default
            });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Preview_DoesNotReplaceAuthoritativeDataAndSurvivesUpdates()
        {
            _service.PreviewCosmetics(0, new EquippedCosmetics { Body = "v4-body", Turret = "v4-turret" });
            _service.SetPlayerData(0, new PlayerDisplayData
            {
                DisplayName = "Renamed",
                Level = 7,
                Equipped = new EquippedCosmetics { Body = "v2-body", Turret = "v2-turret" }
            });

            Assert.That(_body.sprite, Is.EqualTo(_database.Find("v4-body", CosmeticType.Body).Sprite));
            Assert.That(_turret.sprite, Is.EqualTo(_database.Find("v4-turret", CosmeticType.Turret).Sprite));
            Assert.That(_service.Models[0].DisplayName, Is.EqualTo("Renamed"));
            Assert.That(_service.Models[0].Level, Is.EqualTo(7));
            Assert.That(_service.Models[0].Equipped.Body, Is.EqualTo("v2-body"));

            _service.ClearCosmeticPreview(0);

            Assert.That(_body.sprite, Is.EqualTo(_database.Find("v2-body", CosmeticType.Body).Sprite));
            Assert.That(_turret.sprite, Is.EqualTo(_database.Find("v2-turret", CosmeticType.Turret).Sprite));
        }

        [Test]
        public void ReturningSlot_ClearsItsOverride()
        {
            _service.PreviewCosmetics(0, new EquippedCosmetics { Body = "v4-body", Turret = "v4-turret" });
            _service.Return(0);

            Assert.That(_body.sprite, Is.EqualTo(_database.Find("v3-body", CosmeticType.Body).Sprite));
            Assert.That(_turret.sprite, Is.EqualTo(_database.Find("v3-turret", CosmeticType.Turret).Sprite));
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
