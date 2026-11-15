using NUnit.Framework;
using System;
using System.Diagnostics;
using Tanks.Core.Common.Processes;

namespace Tanks.Core.Tests
{
    public sealed class HostProcessServiceTests
    {
        [Test]
        public void CurrentArguments_IdentifyRunningHost()
        {
            string[] arguments = HostProcessService.GetCurrentArguments().Split(' ');
            HostProcessService host = HostProcessService.FromArguments(arguments);

            Assert.True(host.IsRunning());
        }

        [Test]
        public void WithoutHostArgument_DoesNotMonitorStandaloneServer()
        {
            Assert.Null(HostProcessService.FromArguments(Array.Empty<string>()));
        }

        [TestCase(HostProcessService.ARGUMENT)]
        [TestCase(HostProcessService.ARGUMENT, "invalid", "invalid")]
        [TestCase(HostProcessService.ARGUMENT, "0", "0")]
        public void InvalidIdentity_IsRejected(params string[] arguments)
        {
            Assert.Throws<ArgumentException>(() =>
                HostProcessService.FromArguments(arguments));
        }

        [Test]
        public void ReusedProcessId_IsNotTheOriginalHost()
        {
            using Process process = Process.GetCurrentProcess();

            HostProcessService host = new HostProcessService(
                process.Id, process.StartTime.ToUniversalTime().Ticks + 1);

            Assert.False(host.IsRunning());
        }

        [Test]
        public void MissingProcess_IsNotRunning()
        {
            HostProcessService host = new HostProcessService(int.MaxValue, 1);

            Assert.False(host.IsRunning());
        }
    }
}
