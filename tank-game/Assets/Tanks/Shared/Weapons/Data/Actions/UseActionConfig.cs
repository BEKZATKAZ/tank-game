using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Shared.Weapons.Data.Actions
{
    public interface IUseActionConfig
    {
        IUseAction Create(ITransform muzzlePoint);
    }

    public abstract class UseActionConfig : ScriptableObject, IUseActionConfig
    {
        public abstract IUseAction Create(ITransform muzzlePoint);
    }
}
