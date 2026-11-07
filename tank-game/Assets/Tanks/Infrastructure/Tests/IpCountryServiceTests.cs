using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Tanks.Infrastructure.Tests
{
    public sealed class IpCountryServiceTests
    {
        [Test]
        public async Task ConcurrentLookups_ShareRequestAndCacheSuccess()
        {
            int requests = 0;
            var pending = new UniTaskCompletionSource<string>();
            var service = new IpCountryService(() =>
            {
                requests++;
                return pending.Task;
            });

            UniTask<string> first = service.GetCountryCodeAsync();
            UniTask<string> second = service.GetCountryCodeAsync();
            Assert.AreEqual(1, requests);

            pending.TrySetResult("kz");

            Assert.AreEqual("KZ", await first);
            Assert.AreEqual("KZ", await second);
            Assert.AreEqual("KZ", await service.GetCountryCodeAsync());
            Assert.AreEqual(1, requests);
        }

        [Test]
        public async Task Timeout_UsesNeutralThenAllowsNextRequest()
        {
            int requests = 0;
            var service = new IpCountryService(() => ++requests == 1
                ? UniTask.FromException<string>(new TimeoutException())
                : UniTask.FromResult("KZ"));

            Assert.AreEqual("", await service.GetCountryCodeAsync());
            Assert.AreEqual("KZ", await service.GetCountryCodeAsync());
            Assert.AreEqual(2, requests);
        }

        [TestCase(null)]
        [TestCase("USA")]
        [TestCase("ru-RU")]
        [TestCase("12")]
        public async Task InvalidResponse_UsesNeutral(string response)
        {
            var service = new IpCountryService(() => UniTask.FromResult(response));

            Assert.AreEqual("", await service.GetCountryCodeAsync());
        }

        [Test]
        public async Task RussianCulture_KazakhstanIpUsesKazakhstanFlag()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
                var service = new IpCountryService(() => UniTask.FromResult("KZ"));

                Assert.AreEqual("KZ", await service.GetCountryCodeAsync());
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }
}
