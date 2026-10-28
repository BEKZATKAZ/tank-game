using Cysharp.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Tanks.UI.Entry
{
    public sealed class PrivacyPolicyService : IPrivacyPolicyService
    {
        public const string PolicyUrl = "https://raw.githubusercontent.com/BEKZATKAZ/tank-game/refs/heads/main/tank-game/privacy-policy.md";
        public const string AcceptanceKey = "privacy.accepted-policy-sha256";

        public async UniTask<PrivacyPolicy> FetchAsync(CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(PolicyUrl);
            request.timeout = 20;
            request.SetRequestHeader("Cache-Control", "no-cache");

            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException("Privacy policy download failed.");

            return Parse(request.downloadHandler.text);
        }

        public static PrivacyPolicy Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 262144 ||
                text.StartsWith("# Privacy Policy", StringComparison.Ordinal) == false ||
                text.Contains("**Game:** T.A.N.K.S.") == false ||
                text.Contains("**Last updated:**") == false ||
                text.Contains("bekzatkaz.games@gmail.com") == false ||
                text.Contains("## 12. Contact Us") == false)
                throw new FormatException("Invalid privacy policy document.");

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return new PrivacyPolicy(text, BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
        }

        public bool IsAccepted(PrivacyPolicy policy)
        {
            return policy != null && PlayerPrefs.GetString(AcceptanceKey, "") == policy.Hash;
        }

        public void Accept(PrivacyPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            PlayerPrefs.SetString(AcceptanceKey, policy.Hash);
            PlayerPrefs.Save();
        }
    }
}
