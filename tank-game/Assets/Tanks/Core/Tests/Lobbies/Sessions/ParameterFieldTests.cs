using NUnit.Framework;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using UnityEngine;

namespace Tanks.Core.Tests.Lobbies.Sessions
{
    public class ParameterFieldTests
    {
        [Test]
        public void CorrectValue_Valid_KeepsOriginal()
        {
            PossibleIntegers integers = new PossibleIntegers(1, 2, 3, 5, 8, 13);

            for (int i = 0; i < integers.Values.Length; i++)
            {
                int actual = integers.CorrectValue(integers.Values[i]);
                Assert.AreEqual(integers.Values[i], actual);
            }

            PossibleVectorIntegers vectors = new PossibleVectorIntegers(new(1, 2), new(3, 4));

            for (int i = 0; i < vectors.Values.Length; i++)
            {
                Vector2Int actual = vectors.CorrectValue(vectors.Values[i]);
                Assert.AreEqual(vectors.Values[i], actual);
            }
        }

        [Test]
        public void CorrectValue_Invalid_ReturnsDefault()
        {
            PossibleIntegers integers = new PossibleIntegers(1, 2, 3, 5, 8, 13);
            PossibleVectorIntegers vectors = new PossibleVectorIntegers(new(1, 2), new(3, 4));

            int actualInteger = integers.CorrectValue(4);
            Assert.AreEqual(integers.Default, actualInteger);

            Vector2Int actualVector = vectors.CorrectValue(new Vector2Int(1, 3));
            Assert.AreEqual(vectors.Default, actualVector);
        }

        [TestCase(MapSize.Small, 5)]
        [TestCase(MapSize.Medium, 7)]
        [TestCase(MapSize.Large, 9)]
        [TestCase((MapSize)(-1), 7)]
        [TestCase((MapSize)100, 7)]
        public void FromEnum_ReturnsFromArrayOrDefault(MapSize e, int expected)
        {
            var integers = new PossibleIntegers(5, 7, 9)
            {
                Default = 7
            };

            var vectors = new PossibleVectorIntegers(new(5, 5), new(7, 7), new(9, 9))
            {
                Default = new(7, 7)
            };

            Assert.AreEqual(expected, integers.GetValueFromValue(e));
            Assert.AreEqual(expected, vectors.GetValueFromEnum(e).x);
            Assert.AreEqual(expected, vectors.GetValueFromEnum(e).y);
        }

        [Test]
        public void GetDefaultAs_ReturnsDefaultAsEnum()
        {
            var integers = new PossibleIntegers(5, 7, 9)
            {
                Default = 9
            };

            var integersWrong = new PossibleIntegers(5, 7, 9)
            {
                Default = 2
            };

            var vectors = new PossibleVectorIntegers(new(5, 5), new(7, 7), new(9, 9))
            {
                Default = new(7, 7)
            };

            var vectorsWrong = new PossibleVectorIntegers(new(5, 5), new(7, 7), new(9, 9))
            {
                Default = new(3, 3)
            };

            Assert.AreEqual(MapSize.Large, integers.GetDefaultAs<MapSize>());
            Assert.AreEqual(MapSize.Medium, vectors.GetDefaultAs<MapSize>());

            Assert.AreEqual(default(MapSize), integersWrong.GetDefaultAs<MapSize>());
            Assert.AreEqual(default(MapSize), vectorsWrong.GetDefaultAs<MapSize>());
        }
    }
}
