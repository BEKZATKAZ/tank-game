using Cysharp.Threading.Tasks;
using LitMotion;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.UI.Menus.Sections
{
    public sealed class MenuSectionService : MonoBehaviour, IMenuSectionService
    {
        [SerializeField] private Dictionary<MenuSectionType, ViewBase> _sections;

        private MenuSectionType _current;
        private MotionHandle _motion;

        public async void OpenSection(MenuSectionType type, bool instant = false)
        {
            _sections.TryGetValue(_current, out ViewBase currentSection);
            _sections.TryGetValue(type, out ViewBase newSection);

            _current = type;
            _motion.TryCancel();

            if (currentSection)
            {
                currentSection.Root.enabledSelf = false;

                if (instant == false)
                {
                    _motion = LMotion.Create(1f, 0f, duration: 0.5f)
                        .Bind(x => currentSection.Root.style.opacity = x);

                    await _motion;
                }

                currentSection.Root.style.opacity = 0;
                currentSection.Hide();
            }

            if (newSection == false || _current != type) return;

            newSection.Show();
            newSection.Root.enabledSelf = true;

            if (instant == false)
            {
                _motion = LMotion.Create(0f, 1f, duration: 0.5f)
                    .Bind(x => newSection.Root.style.opacity = x);

                await _motion;
            }

            newSection.Root.style.opacity = 1;
        }

        private async void Start()
        {
            await UniTask.Delay(2000);
            OpenSection(MenuSectionType.Main);
        }
    }
}
