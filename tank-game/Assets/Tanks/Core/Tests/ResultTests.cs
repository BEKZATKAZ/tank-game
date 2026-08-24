using NUnit.Framework;

namespace Tanks.Core.Tests
{
    public class ResultTests
    {
        [Test]
        public void Map_Success_ConvertsSuccessfully()
        {
            ResultType<int> result = Result.Success(100);
            ResultType<float> mapped = Result.Map(result, x => (float)x);
            Assert.AreEqual(100f, mapped.Value, "Mapped incorrectly");
        }

        [Test]
        public void Map_Failure_DoesNotMap()
        {
            ResultType<int> result = Result.Failure<int>("some error");
            ResultType<float> mapped = Result.Map(result, x => (float)x);
            Assert.AreEqual("some error", mapped.ErrorCode, "Should be an error");
        }

        [Test]
        public void Success_WithoutValue()
        {
            ResultType result = Result.Success();
            Assert.True(result.IsSuccess, "IsSuccess should be true when succeeded");
            Assert.Null(result.ErrorCode, "Error should be null when succeeded");
        }

        [Test]
        public void Success_WithValue()
        {
            ResultType<float> result = Result.Success(42f);
            Assert.AreEqual(42, result.Value);
            Assert.True(result.IsSuccess, "IsSuccess should be true when succeeded");
            Assert.Null(result.ErrorCode, "Error should be null when succeeded");

            ResultType<float> resultConverted = 50;
            Assert.AreEqual(50, resultConverted.Value);
            Assert.True(resultConverted.IsSuccess, "IsSuccess should be true when succeeded");
            Assert.Null(resultConverted.ErrorCode, "Error should be null when succeeded");
        }

        [Test]
        public void Failure_WithValue_ConvertToWithoutValue()
        {
            ResultType<float> result = Result.Failure<float>("converted error");
            ResultType resultConverted = result;

            Assert.False(resultConverted.IsSuccess, "IsSuccess should be false when failed");
            Assert.AreEqual("converted error", resultConverted.ErrorCode);
        }

        [Test]
        public void Failure_WithoutValue()
        {
            ResultType result = Result.Failure("some error");
            Assert.AreEqual("some error", result.ErrorCode);
            Assert.False(result.IsSuccess, "IsSuccess should be false when failed");
        }

        [Test]
        public void Failure_WithValue()
        {
            ResultType<float> result = Result.Failure<float>("some error");
            Assert.AreEqual("some error", result.ErrorCode);
            Assert.False(result.IsSuccess, "IsSuccess should be false when failed");
        }
    }
}
