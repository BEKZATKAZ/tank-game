using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Tanks.Core.Helpers;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Bootstrap
{
    public interface IGameplayScope
    {
        void Configure(IContainerBuilder builder);
    }

    public interface IGameplayScope<T> : IGameplayScope where T : GameplayScopeBase
    {

    }

    public abstract class GameplayScopeBase : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            IEnumerable<Type> types = SplitComponentFinder.FindFor(
                this, typeof(IGameplayScope<>));

#if UNITY_EDITOR
            // Filtering since all assemblies are loaded in the editor
            // This logic is not necessary in builds
            if (CurrentPlayer.Tags.Any(x => x.Equals("server", StringComparison.OrdinalIgnoreCase)))
            {
                types = types.Where(x => IsSharedOr(x.Assembly, "server"));
            }
            else
            {
                types = types.Where(x => IsSharedOr(x.Assembly, "client"));
            }
#endif

            foreach (Type type in types)
            {
                try
                {
                    IGameplayScope scope = Activator.CreateInstance(type) as IGameplayScope;
                    scope.Configure(builder);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static bool IsSharedOr(Required<Assembly> assembly, string expectedName)
        {
            string pattern = $"shared|{expectedName}";
            return Regex.IsMatch(assembly.Value.FullName, pattern, RegexOptions.IgnoreCase);
        }
    }
}