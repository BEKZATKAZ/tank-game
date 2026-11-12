using Cysharp.Threading.Tasks;
using LitMotion;
using System;
using System.Collections.Generic;
using Tanks.Client.Rendering;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Tanks.Shared.Adapters;
using Tanks.UI.Menus.Dialogs;
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
        [Inject] private readonly GameplaySessionService _session;
        [Inject] private readonly IDialogScreenService _dialog;
        [Inject] private readonly IPendingDebriefingService _debriefing;

        private MotionHandle _motion;

        public MenuSectionType Current { get; private set; }

        public async void OpenSection(
            MenuSectionType type, bool instant = false, float menuDelay = 0)
        {
            if (_loading.IsReady.Value == false) return;

            _sections.TryGetValue(Current, out ViewBase currentSection);
            _sections.TryGetValue(type, out ViewBase newSection);
            _cameraCenters.TryGetValue(type, out Transform cameraCenter);

            Current = type;
            _motion.TryCancel();

            if (instant && cameraCenter)
            {
                _cameraFollow.SetFollowTarget(new UnityTransform(cameraCenter), instant: false);
            }

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

            if (menuDelay > 0) await UniTask.Delay(TimeSpan.FromSeconds(menuDelay));
            if (newSection == false || Current != type) return;

            newSection.Show();
            newSection.Root.enabledSelf = true;

            if (instant == false)
            {
                if (cameraCenter)
                {
                    _cameraFollow.SetFollowTarget(new UnityTransform(cameraCenter), instant: false);
                }

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
            if (ready == false) return;

            OpenSection(_debriefing.HasPending
                ? MenuSectionType.Debriefing
                : MenuSectionType.Main, menuDelay: 1);

            string error = _session.ConnectionError;
            _session.ConnectionError = null;

            if (string.IsNullOrWhiteSpace(error)) return;
            _dialog.ShowErrorFromCode("error.lobby", error);
        }

        private void OnDestroy()
        {
            _loading.IsReady.Unbind(OnReady);
        }
    }
}
