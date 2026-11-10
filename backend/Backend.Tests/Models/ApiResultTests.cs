using Backend.Models;
using Newtonsoft.Json;

namespace Backend.Tests.Models;

public class ApiResultTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void JsonRoundTrip_PreservesSuccessAndFailure(bool faulted)
    {
        ApiResult expected = faulted ? ApiResult.Fail("failed") : ApiResult.Ok();
        ApiResult<int> generic = faulted ? ApiResult.Fail<int>("failed") : ApiResult.Ok(42);

        string json = JsonConvert.SerializeObject(expected);
        string genericJson = JsonConvert.SerializeObject(generic);

        Assert.Multiple(() =>
        {
            Assert.That(JsonConvert.DeserializeObject<ApiResult>(json), Is.EqualTo(expected));
            Assert.That(JsonConvert.DeserializeObject<ApiResult<int>>(genericJson), Is.EqualTo(generic));
            Assert.That(System.Text.Json.JsonSerializer.Deserialize<ApiResult>(
                System.Text.Json.JsonSerializer.Serialize(expected)), Is.EqualTo(expected));

            Assert.That(System.Text.Json.JsonSerializer.Deserialize<ApiResult<int>>(
                System.Text.Json.JsonSerializer.Serialize(generic)), Is.EqualTo(generic));
        });
    }

    [Test]
    public void Ok_ReportsSuccess()
    {
        ApiResult result = ApiResult.Ok();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFaulted, Is.False);
            Assert.That(result.ErrorCode, Is.Null);
        });
    }

    [TestCase("not-found")]
    [TestCase("")]
    public void Fail_PreservesError(string error)
    {
        ApiResult result = ApiResult.Fail(error);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFaulted, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo(error));
        });
    }

    [Test]
    public void GenericFactories_PreserveValueOrError()
    {
        object value = new();
        ApiResult<object> success = ApiResult.Ok(value);
        ApiResult<object> failure = ApiResult.Fail<object>("failed");

        Assert.Multiple(() =>
        {
            Assert.That(success.Value, Is.SameAs(value));
            Assert.That(success.IsSuccess, Is.True);
            Assert.That(success.IsFaulted, Is.False);
            Assert.That(success.ErrorCode, Is.Null);
            Assert.That(failure.Value, Is.Null);
            Assert.That(failure.IsSuccess, Is.False);
            Assert.That(failure.IsFaulted, Is.True);
            Assert.That(failure.ErrorCode, Is.EqualTo("failed"));
        });
    }

    [TestCase(null)]
    [TestCase("failed")]
    public async Task RunAsync_PreservesResult(string? error)
    {
        ApiResult expected = new(error);

        ApiResult result = await ApiResultExtensions.RunAsync(
            () => Task.FromResult(expected));

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("failed")]
    public async Task GenericRunAsync_PreservesResult(string? error)
    {
        ApiResult<int> expected = new(42, error);

        ApiResult<int> result = await ApiResultExtensions.RunAsync(
            () => Task.FromResult(expected));

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(false, "unknown")]
    [TestCase(true, "unknown")]
    [TestCase(false, "custom")]
    [TestCase(true, "custom")]
    public async Task RunAsync_ExceptionsBecomeErrors(bool synchronous, string error)
    {
        Task<ApiResult> Run()
        {
            if (synchronous) throw new InvalidOperationException();
            return Task.FromException<ApiResult>(new InvalidOperationException());
        }

        Task<ApiResult<int>> RunGeneric()
        {
            if (synchronous) throw new InvalidOperationException();
            return Task.FromException<ApiResult<int>>(new InvalidOperationException());
        }

        ApiResult result = error == "unknown"
            ? await ApiResultExtensions.RunAsync(Run)
            : await ApiResultExtensions.RunAsync(Run, error);

        ApiResult<int> generic = error == "unknown"
            ? await ApiResultExtensions.RunAsync(RunGeneric)
            : await ApiResultExtensions.RunAsync(RunGeneric, error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ApiResult.Fail(error)));
            Assert.That(generic, Is.EqualTo(ApiResult.Fail<int>(error)));
        });
    }
}
