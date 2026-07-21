using NUnit.Framework;
using Tanks.Shared.Items;

namespace Tanks.Shared.Tests.Items
{
    public class ItemIdentifierTests
    {
        [TestCase("test")]
        [TestCase("some_id_123")]
        [TestCase("a")]
        [TestCase("health_up")]
        public void ValidInput_CreatesId(string expected)
        {
            ItemIdentifier id = new ItemIdentifier(expected);

            Assert.AreEqual(expected.Length, id.Length, "Length dismatch, encoding problems?");
            Assert.AreEqual(expected, id.ToString(), "Content dismatch");
            Assert.AreEqual(id, id, "Should be equal");
        }
    }
}
