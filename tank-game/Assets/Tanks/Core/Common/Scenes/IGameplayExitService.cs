using Cysharp.Threading.Tasks;

namespace Tanks.Core.Common.Scenes
{
    public interface IGameplayExitService
    {
        UniTask<ResultType> ExitAsync();
    }
}
