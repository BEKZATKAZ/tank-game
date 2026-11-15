using System;
using System.Diagnostics;
using System.Globalization;

namespace Tanks.Core.Common.Processes
{
    public sealed class HostProcessService
    {
        public HostProcessService(int processId, long startTime)
        {
            _processId = processId;
            _startTime = startTime;
        }

        public const string ARGUMENT = "-hostProcess";

        private readonly int _processId;
        private readonly long _startTime;

        public static string GetCurrentArguments()
        {
            using Process process = Process.GetCurrentProcess();

            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}",
                ARGUMENT, process.Id, process.StartTime.ToUniversalTime().Ticks);
        }

        public static HostProcessService FromArguments(string[] arguments)
        {
            int index = Array.IndexOf(arguments, ARGUMENT);

            if (index < 0) return null;

            if (index + 2 >= arguments.Length ||
                int.TryParse(arguments[index + 1], out int processId) == false ||
                long.TryParse(arguments[index + 2], out long startTime) == false ||
                processId <= 0 || startTime <= 0)
            {
                throw new ArgumentException("Invalid host process identity.");
            }

            return new HostProcessService(processId, startTime);
        }

        public bool IsRunning()
        {
            try
            {
                using Process process = Process.GetProcessById(_processId);

                return process.HasExited == false &&
                    process.StartTime.ToUniversalTime().Ticks == _startTime;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
