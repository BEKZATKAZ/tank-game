using NUnit.Framework;

namespace Tanks.Utils.Tests
{
    public class RingBufferTests
    {
        private RingBuffer<int> _buffer;

        [SetUp]
        public void Setup() => _buffer = new RingBuffer<int>(128);

        [TearDown]
        public void Teardown() => _buffer.Dispose();

        [Test]
        public void Overflow_WrapsBack()
        {
            for (int i = 0; i < 132; i++) _buffer[i] = i;

            for (int i = 0; i < 128; i++)
            {
                int expected = i < 4 ? i + 128 : i;
                Assert.AreEqual(expected, _buffer[i]);
            }
        }
    }
}
