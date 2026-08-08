using Cysharp.Threading.Tasks;

namespace Tanks.Core.Repositories
{
    public interface IFileSystem
    {
        bool Exists(string path);

        string ReadAllText(string path);
        void WriteAllText(string path, string contents);

        UniTask<string> ReadAllTextAsync(string path);
        UniTask WriteAllTextAsync(string path, string contents);
    }
}