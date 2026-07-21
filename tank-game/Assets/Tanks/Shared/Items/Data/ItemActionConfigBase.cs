using UnityEngine;
using VContainer;

namespace Tanks.Shared.Items.Data
{
    public abstract class ItemActionConfigBase : ScriptableObject
    {
        public abstract IItemAction Create(IObjectResolver resolver);
    }
}
