using LitMotion;
using System.Collections.Generic;
using Tanks.Client.Rendering;
using Tanks.Core.Players;
using Tanks.Shared.Adapters;
using UnityEngine;
using VContainer;

namespace Tanks.UI.Menus.Sections
{
    public sealed class MenuSectionService : MonoBehaviour, IMenuSectionService
    {
        [SerializeField] private Dictionary<MenuSectionType, ViewBase> _sections;
        [SerializeField] private Dictionary<MenuSectionType, Transform> _cameraCenters;

        [Inject] private readonly ICameraFollowService _cameraFollow;
        [Inject] private readonly MenuLoadingState _loading;

        private MotionHandle _motion;

        public MenuSectionType Current { get; private set; }

        public async void OpenSection(MenuSectionType type, bool instant = false)
        {
            if (_loading.IsReady.Value == false) return;

            _sections.TryGetValue(Current, out ViewBase currentSection);
            _sections.TryGetValue(type, out ViewBase newSection);

            Current = type;
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

            if (newSection == false || Current != type) return;

            newSection.Show();
            newSection.Root.enabledSelf = true;

            if (_cameraCenters.TryGetValue(type, out Transform cameraCenter))
            {
                _cameraFollow.SetFollowTarget(new UnityTransform(cameraCenter), instant);
            }

            if (instant == false)
            {
                _motion = LMotion.Create(0f, 1f, duration: 0.5f)
                    .Bind(x => newSection.Root.style.opacity = x);

                await _motion;
            }

            newSection.Root.style.opacity = 1;
        }

        private void Start()
        {
            _cameraFollow.SetBounds(new Bounds(default, new Vector3(256, 256)));
            _loading.IsReady.Bind(OnReady);
        }

        private void OnReady(bool ready)
        {
            if (ready) OpenSection(MenuSectionType.Main);
        }

        private void OnDestroy()
        {
            _loading.IsReady.Unbind(OnReady);
        }
    }
}
