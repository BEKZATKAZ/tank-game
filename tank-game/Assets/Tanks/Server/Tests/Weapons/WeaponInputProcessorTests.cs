using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Weapons;
using Tanks.Utils;

namespace Tanks.Server.Tests.Weapons
{
    public class WeaponInputProcessorTests
    {
        private readonly WeaponInputProcessor _processor = new();

        [TestCase(60)]
        [TestCase(70)]
        [TestCase(90)]
        [TestCase(94)]
        [TestCase(99)]
        public void ProcessesMissedInputs(int lastTick)
        {
            RingBuffer8<ClientShot> buffer = new RingBuffer8<ClientShot>(
                new ClientShot(tick: 71, new ShotId(1)),
                new ClientShot(tick: 81, new ShotId(2)),
                new ClientShot(tick: 89, new ShotId(3)),
                new ClientShot(tick: 90, new ShotId(4)),
                new ClientShot(tick: 93, new ShotId(5)),
                new ClientShot(tick: 94, new ShotId(6)),
                new ClientShot(tick: 99, new ShotId(7)),
                new ClientShot(tick: 100, new ShotId(8)));

            WeaponInput input = new WeaponInput { FireBuffer = buffer };

            int clientTick = buffer[^1].ClientTick;
            int callbackCount = 0;
            int index = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ClientTick <= lastTick) continue;
                index = i;
                break;
            }

            _processor.Process(input, lastTick, (shot) =>
            {
                Assert.AreEqual(buffer[index].Id, shot.Id);
                Assert.AreEqual(buffer[index].ClientTick, shot.ClientTick);

                index++;
                callbackCount++;
            });

            int expectedCallbackCount = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ClientTick <= lastTick) continue;
                expectedCallbackCount++;
            }

            Assert.AreEqual(expectedCallbackCount, callbackCount, "Callback count dismatch");
        }
    }
}
