using NUnit.Framework;
using System;

namespace Tanks.Utils.Tests
{
    public class FixedRingBufferTests
    {
        [Test]
        public void Add_Shifts()
        {
            RingBuffer8<int> buffer = default;

            buffer = buffer.Add(5);
            Assert.AreEqual(5, buffer.H, "Should start from value D");

            // Fill
            buffer = buffer.Add(0).Add(0).Add(4);

            Assert.AreEqual(5, buffer.E, "Value E was written incorrectly");
            Assert.AreEqual(0, buffer.F, "Value F was written incorrectly");
            Assert.AreEqual(0, buffer.G, "Value G was written incorrectly");
            Assert.AreEqual(4, buffer.H, "Value H was written incorrectly");

            // Overflow
            buffer = buffer.Add(3).Add(0).Add(0).Add(1).Add(2);

            Assert.AreEqual(0, buffer.A, "Value A was overwritten incorrectly");
            Assert.AreEqual(0, buffer.B, "Value B was overwritten incorrectly");
            Assert.AreEqual(4, buffer.C, "Value C was overwritten incorrectly");
            Assert.AreEqual(3, buffer.D, "Value D was overwritten incorrectly");

            Assert.AreEqual(0, buffer.E, "Value E was overwritten incorrectly");
            Assert.AreEqual(0, buffer.F, "Value F was overwritten incorrectly");
            Assert.AreEqual(1, buffer.G, "Value G was overwritten incorrectly");
            Assert.AreEqual(2, buffer.H, "Value H was overwritten incorrectly");
        }

        [Test]
        public void Indexer_ReturnsElements()
        {
            RingBuffer8<bool> buffer = default;

            buffer = buffer
                .Add(true)
                .Add(false)
                .Add(true);

            // Not assigned
            Assert.False(buffer[0], "Wrong A");
            Assert.False(buffer[1], "Wrong B");
            Assert.False(buffer[2], "Wrong C");
            Assert.False(buffer[3], "Wrong D");
            Assert.False(buffer[4], "Wrong E");

            // Assigned
            Assert.True(buffer[5], "Wrong F");
            Assert.False(buffer[6], "Wrong G");
            Assert.True(buffer[7], "Wrong H");
        }

        [TestCase(-1)]
        [TestCase(-4)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void Indexer_OutOfRange_Throws(int index)
        {
            RingBuffer8<bool> buffer = default;
            Assert.Throws<IndexOutOfRangeException>(() => { var a = buffer[index]; });
        }
    }
}
