using System;
using System.Collections.Generic;
using Unity.Multiplayer;
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
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            bool isServer = role is MultiplayerRoleFlags.Server;

            IEnumerable<Type> types = SplitComponentFinder.FindFor(
                this, typeof(IGameplayScope<>), isServer);

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
    }
}
