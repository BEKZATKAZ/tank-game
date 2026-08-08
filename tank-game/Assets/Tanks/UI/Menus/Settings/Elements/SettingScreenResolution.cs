using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    [UxmlElement]
    public sealed partial class SettingScreenResolution : SettingDropdown
    {
        public SettingScreenResolution()
        {
            ChoiceKeys = Screen.resolutions.Select(x => x.ToString()).ToList();
        }
    }
}
