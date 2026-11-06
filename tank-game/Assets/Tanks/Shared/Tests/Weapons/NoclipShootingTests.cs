using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Helpers;
using Tanks.Shared.Effects;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons
{
    public sealed class NoclipShootingTests
    {
        [Test]
        public void Noclip_BlocksShotsWithoutConsumingAmmoAndResumesAfterExpiry()
        {
            GameObject player = new("noclip-shooting-test");
            WeaponHolderBehaviour holder = player.AddComponent<WeaponHolderBehaviour>();
            EffectOwnerBehaviour effects = player.AddComponent<EffectOwnerBehaviour>();
            typeof(WeaponHolderBehaviour).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(holder, null);
            typeof(WeaponHolderBehaviour).GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(holder, Substitute.For<IMessageBus>());

            Weapon weapon = new();
            weapon.Initialize("noclip-test", new FixedAmmo(2), new Cooldown(0.2f),
                new FullAutomaticUseMode(), new NoUseAction(), Substitute.For<IRecoilConfig>().AsRequired());
            holder.SetDefault(weapon);
            holder.SwitchToDefault();

            try
            {
                effects.AddEffect(EffectType.Noclip, 6);

                Assert.That(holder.WeaponTick(1, true, new ShotId(1)), Is.False);
                Assert.That(weapon.Ammo.Current, Is.EqualTo(2));

                effects.OnTick(6, 1);

                Assert.That(holder.WeaponTick(1, true, new ShotId(2)), Is.True);
                Assert.That(weapon.Ammo.Current, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(player); }
        }
    }
}
