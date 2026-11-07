using Cysharp.Threading.Tasks;

namespace Tanks.Core
{
    public interface ICountryService
    {
        UniTask<string> GetCountryCodeAsync();
    }
}
