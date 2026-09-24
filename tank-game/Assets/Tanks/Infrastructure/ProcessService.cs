using System;
using System.Diagnostics;
using System.IO;
using Tanks.Core.Common.Processes;

namespace Tanks.Infrastructure
{
    public sealed class ProcessService : IProcessService
    {
        public int Launch(
            string path,
            string arguments = "",
            Action<string> stdout = null,
            Action<string> stderr = null)
        {
            string fullPath = Path.GetFullPath(path);

            UnityEngine.Debug.Log($"Launching process: '{fullPath}'");

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetDirectoryName(fullPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = stdout != null,
                    RedirectStandardError = stderr != null
                }
            };

            if (stdout != null)
            {
                process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout(e.Data); };
            }

            if (stderr != null)
            {
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr(e.Data); };
            }

            process.Start();

            if (stdout != null) process.BeginOutputReadLine();
            if (stderr != null) process.BeginErrorReadLine();

            return process.Id;
        }

        public void KillById(int id)
        {
            try
            {
                Process process = Process.GetProcessById(id);
                if (process?.HasExited == false) process.Kill();
            }
            catch (ArgumentException ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
