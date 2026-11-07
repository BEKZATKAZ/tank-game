using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Leaderboards;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

namespace Tanks.Infrastructure
{
    public sealed class IpCountryService : ICountryService
    {
        [Inject]
        public IpCountryService() : this(FetchCountryAsync) { }

        public IpCountryService(Func<UniTask<string>> lookup)
        {
            _lookup = lookup;
        }

        [Serializable]
        private sealed class CountryResponse
        {
            public string country;
        }

        private readonly Func<UniTask<string>> _lookup;

        private string _countryCode;
        private Task<string> _pending;

        public UniTask<string> GetCountryCodeAsync()
        {
            if (string.IsNullOrEmpty(_countryCode) == false) return UniTask.FromResult(_countryCode);

            _pending ??= ResolveAsync().AsTask();
            return AwaitLookupAsync(_pending);
        }

        private async UniTask<string> AwaitLookupAsync(Task<string> pending)
        {
            try
            {
                string code = await pending;
                if (code.Length > 0) _countryCode = code;
                return code;
            }
            finally
            {
                if (ReferenceEquals(_pending, pending)) _pending = null;
            }
        }

        private async UniTask<string> ResolveAsync()
        {
            try { return LeaderboardSettings.NormalizeCountryCode(await _lookup()); }
            catch (Exception) { return ""; }
        }

        private static async UniTask<string> FetchCountryAsync()
        {
            using UnityWebRequest request = UnityWebRequest.Get("https://api.country.is/");
            request.timeout = 3;
            await request.SendWebRequest().ToUniTask();

            return JsonUtility.FromJson<CountryResponse>(request.downloadHandler.text)?.country;
        }
    }
}
