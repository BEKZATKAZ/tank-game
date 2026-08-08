using NUnit.Framework;

namespace Tanks.Utils.Tests
{
    public class BindablePropertyTests
    {
        [TestCase(1)]
        [TestCase(42)]
        [TestCase(100)]
        public void Bind_WorksInstantly(int expected)
        {
            BindableProperty<int> property = new BindableProperty<int>(expected);
            int result = 0;

            property.Bind(x => result = x);
            Assert.AreEqual(expected, result, "Should assign value right after binding");

            property.Value = expected + 10;
            Assert.AreEqual(expected + 10, result, "Should update value");
        }

        [Test]
        public void Bind_MultipleBindings_WorksCorrectly()
        {
            BindableProperty<int> property = new BindableProperty<int>(100);

            int result1 = 1;
            int result2 = 2;
            int result3 = 3;

            property.Bind(x => result1 = x);
            property.Bind(x => result2 = x + 1);
            property.Bind(x => result3 = x + 2);
            property.Value = 5;

            Assert.AreEqual(5, result1, "Should update value");
            Assert.AreEqual(6, result2, "Should update value");
            Assert.AreEqual(7, result3, "Should update value");
        }

        [Test]
        public void Unbind_WorksCorrectly()
        {
            BindableProperty<int> property = new BindableProperty<int>(42);

            int result = 0;
            void binding(int x) => result = x;

            property.Bind(binding);
            Assert.AreEqual(42, result, "Should assign value right after binding");

            property.Unbind(binding);
            property.Value = 10;

            Assert.AreEqual(42, result, "Should not update after unbinding");
        }
    }
}
