using NUnit.Framework;
using System;
using Tanks.Core.Helpers;
using UnityEngine;

namespace Tanks.Core.Tests.Helpers
{
    public class RequiredTests
    {
        [Test]
        public void NotNull_Passes()
        {
            Assert.DoesNotThrow(() => new Required<object>(new object()),
                "Should not throw because the object is not null!");

            Assert.DoesNotThrow(() => new Required<RequiredTests>(this),
                "Should not throw because the object is not null!");
        }

        [Test]
        public void Null_Throws()
        {
            Assert.Throws<NullReferenceException>(() => new Required<object>(null),
                "Should throw because the object is null!");

            Assert.Throws<NullReferenceException>(() => new Required<RequiredTests>(null),
                "Should throw because the object is null!");
        }

        [Test]
        public void Equals_RunsCorrectly()
        {
            RequiredTests obj = this;
            Required<RequiredTests> objR = this;

            Assert.True(objR == obj);
            Assert.True(obj == objR);

            Assert.False(objR != obj);
            Assert.False(obj != objR);

            Assert.True(objR.Equals(obj));
            Assert.True(objR.Equals(objR));
        }
    }
}
