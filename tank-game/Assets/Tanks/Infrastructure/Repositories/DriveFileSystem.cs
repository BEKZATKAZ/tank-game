using Cysharp.Threading.Tasks;
using System.IO;
using Tanks.Core.Repositories;

namespace Tanks.Infrastructure.Repositories
{
    public sealed class DriveFileSystem : IFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public string ReadAllText(string path) => File.ReadAllText(path);
        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

        public async UniTask<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public async UniTask WriteAllTextAsync(string path, string contents)
        {
            await File.WriteAllTextAsync(path, contents);
        }
    }
}
