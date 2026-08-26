using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Utils;

namespace Tanks.Server.Tests.Weapons
{
    public sealed class WeaponInputSchedulerTests
    {
        private WeaponInputScheduler _scheduler;
        private Weapon _weapon;

        private ScheduledShot[] _expected;
        private int _expectedIndex;

        private bool OnShoot(ScheduledShot shot, float _)
        {
            ScheduledShot expected = _expected[_expectedIndex++];

            Assert.AreEqual(expected.Id, shot.Id, "Shot ID dismatch");
            Assert.AreEqual(expected.Tick, shot.Tick, "Shot tick dismatch");

            return true;
        }

        [SetUp]
        public void Setup()
        {
            _weapon = new Weapon();

            _weapon.Initialize(
                new FixedAmmo(10),
                new Cooldown(0.15f),
                new FullAutomaticUseMode(),
                new NoUseAction(),
                new RecoilConfig());

            _scheduler = new WeaponInputScheduler(OnShoot, null);
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler.Dispose();
        }

        [Test]
        public void Schedule_RespectsCooldown()
        {
            RingBuffer8<ClientShot> buffer = default;

            buffer = buffer.Add(new ClientShot(10, new ShotId(1)));
            buffer = buffer.Add(new ClientShot(11, new ShotId(2)));
            buffer = buffer.Add(new ClientShot(12, new ShotId(3)));

            WeaponInput input = new()
            {
                FireBuffer = buffer
            };

            // Cooldown = 0.15 / 0.05 = 3 ticks
            _expected = new[]
            {
                new ScheduledShot(9, new ShotId(1)),
                new ScheduledShot(12, new ShotId(2)),
                new ScheduledShot(15, new ShotId(3)),
            };

            _scheduler.Schedule(_weapon, input, 9, 0.05f);

            Assert.AreEqual(3, _scheduler.ScheduledCount);

            for (int tick = 9; tick <= 15; tick++) _scheduler.Tick(tick, 0.05f);

            Assert.AreEqual(3, _expectedIndex);
        }

        [Test]
        public void Schedule_IgnoresAlreadyProcessedShots()
        {
            RingBuffer8<ClientShot> bufferA = default;

            bufferA = bufferA.Add(new ClientShot(10, new ShotId(1)));
            bufferA = bufferA.Add(new ClientShot(11, new ShotId(2)));

            WeaponInput inputA = new()
            {
                FireBuffer = bufferA
            };

            _scheduler.Schedule(_weapon, inputA, 9, 0.05f);

            Assert.AreEqual(2, _scheduler.ScheduledCount);

            RingBuffer8<ClientShot> bufferB = default;

            bufferB = bufferB.Add(new ClientShot(10, new ShotId(1)));
            bufferB = bufferB.Add(new ClientShot(11, new ShotId(2)));
            bufferB = bufferB.Add(new ClientShot(12, new ShotId(3)));

            WeaponInput inputB = new()
            {
                FireBuffer = bufferB
            };

            _scheduler.Schedule(_weapon, inputB, 10, 0.05f);

            Assert.AreEqual(3, _scheduler.ScheduledCount);
        }

        [Test]
        public void Schedule_EmptyInputDoesNothing()
        {
            WeaponInput input = default;

            _scheduler.Schedule(_weapon, input, 10, 0.05f);

            Assert.AreEqual(0, _scheduler.ScheduledCount);
        }
    }
}
