using System.Collections.Generic;
using NUnit.Framework;
using Tanks.Shared.Effects;
using UnityEngine;

namespace Tanks.Shared.Tests.Effects
{
    public sealed class NoclipEffectTests
    {
        private EffectOwnerBehaviour _effects;
        private Rigidbody2D _body;

        [SetUp]
        public void Setup()
        {
            GameObject player = new("noclip-test", typeof(Rigidbody2D));
            _body = player.GetComponent<Rigidbody2D>();
            _body.excludeLayers = 1 << 4;
            _effects = player.AddComponent<EffectOwnerBehaviour>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_effects.gameObject);

        [Test]
        public void Noclip_OnlyExcludesWallsAndRestoresOriginalMaskAtExpiry()
        {
            List<EffectEndReason> reasons = new();
            _effects.Ended += (type, reason) => reasons.Add(reason);
            _effects.AddEffect(EffectType.Noclip, 6);

            Assert.That(_body.excludeLayers.value, Is.EqualTo((1 << 4) | 1));

            _effects.OnTick(6, 1);

            Assert.That(_body.excludeLayers.value, Is.EqualTo(1 << 4));
            Assert.That(reasons, Is.EqualTo(new[] { EffectEndReason.Expired }));
        }

        [Test]
        public void RepeatedNoclip_ExtendsWithoutOverwritingOriginalMask()
        {
            EffectId id = _effects.AddEffect(EffectType.Noclip, 6);
            _effects.OnTick(3, 1);
            _effects.ExtendEffect(id, 6);
            _effects.OnTick(6, 1);

            Assert.That(_effects.Effects.ContainsKey(EffectType.Noclip), Is.True);

            _effects.OnTick(12, 1);

            Assert.That(_effects.Effects, Is.Empty);
            Assert.That(_body.excludeLayers.value, Is.EqualTo(1 << 4));
        }

        [Test]
        public void Burning_PreservesNoclipButReportsFrozenReplacement()
        {
            EffectEndReason? reason = null;
            _effects.Ended += (type, ended) =>
            {
                if (type == EffectType.Frozen) reason = ended;
            };

            _effects.AddEffect(EffectType.Noclip, 6);
            _effects.AddEffect(EffectType.Frozen, 5);
            _effects.AddEffect(EffectType.Burning, 3);

            Assert.That(reason, Is.EqualTo(EffectEndReason.Replaced));
            Assert.That(_effects.Effects.ContainsKey(EffectType.Noclip), Is.True);
        }

        [Test]
        public void IndependentNoclip_KeepsWallBypassUntilFinalIdEnds()
        {
            EffectId first = _effects.AddEffect(EffectType.Noclip, 3);
            EffectId second = _effects.AddEffect(EffectType.Noclip, 6);

            _effects.OnTick(3, 1);

            Assert.That(_effects.HasEffect(first), Is.False);
            Assert.That(_effects.HasEffect(second), Is.True);
            Assert.That(_body.excludeLayers.value, Is.EqualTo((1 << 4) | 1));

            _effects.RemoveEffect(second);

            Assert.That(_body.excludeLayers.value, Is.EqualTo(1 << 4));
        }
    }
}
