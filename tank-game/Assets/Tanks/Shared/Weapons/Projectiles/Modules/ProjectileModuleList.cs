using System;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public sealed class ProjectileModuleList : NetworkBehaviourExtended, IProjectileModuleList
    {
        private ProjectileModuleBehaviourBase[] _modules;

        public void Call(Action<ProjectileModuleBehaviourBase> action)
        {
            if (_modules is not { Length: > 0 }) return;

            foreach (ProjectileModuleBehaviourBase module in _modules)
            {
                try { action(module); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private void Start()
        {
            _modules = GetComponentsInChildren<ProjectileModuleBehaviourBase>(true)
                ?? new ProjectileModuleBehaviourBase[0];
        }
    }
}
