using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tanks.UI.Entry
{
    public interface IPrivacyPolicyService
    {
        UniTask<PrivacyPolicy> FetchAsync(CancellationToken cancellationToken);
        bool IsAccepted(PrivacyPolicy policy);
        void Accept(PrivacyPolicy policy);
    }

    public sealed class PrivacyPolicy
    {
        public PrivacyPolicy(string text, string hash)
        {
            Text = text;
            Hash = hash;
        }

        public string Text { get; }
        public string Hash { get; }
    }
}
