using System;

namespace Tanks.Core.Common.Processes
{
    public interface IProcessService
    {
        int Launch(
            string path,
            string arguments = "",
            Action<string> stdout = null,
            Action<string> stderr = null);

        void KillById(int id);
    }
}
