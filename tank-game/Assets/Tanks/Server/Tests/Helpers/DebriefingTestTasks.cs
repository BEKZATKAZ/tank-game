using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;

namespace Tanks.Server.Tests.Helpers
{
    public static class DebriefingTestTasks
    {
        public static IEnumerator Complete(Task task)
        {
            for (int frame = 0; frame < 30 && task.IsCompleted == false; frame++)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.True,
                "The controlled clock transition did not complete the operation");

            task.GetAwaiter().GetResult();
        }
    }
}
