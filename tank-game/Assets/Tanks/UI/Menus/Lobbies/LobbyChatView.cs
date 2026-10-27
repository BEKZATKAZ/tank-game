using System;
using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.Lobbies.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyChatView : ViewBase<LobbyChatViewModel>
    {
        private VisualElement _root;
        private ScrollView _messages;
        private TextField _field;
        private Button _sendButton;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Chat");
            _messages = _root.Q<ScrollView>();
            _field = _root.Q<TextField>("ChatField");
            _sendButton = _root.Q<Button>("SendButton");
            _field.maxLength = ILobbyChatService.MaxMessageLength;
            _field.multiline = false;

            _field.RegisterValueChangedCallback(OnDraftChanged);
            _field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _sendButton.clicked += Send;
            _messages.Clear();
        }

        protected override void Bind(LobbyChatViewModel viewModel)
        {
            viewModel.Messages.Bind(OnMessagesChanged);
            viewModel.Draft.Bind(OnDraftUpdated);
            viewModel.CanSend.Bind(OnCanSendChanged);
        }

        protected override void Unbind(LobbyChatViewModel viewModel)
        {
            viewModel.Messages.Unbind(OnMessagesChanged);
            viewModel.Draft.Unbind(OnDraftUpdated);
            viewModel.CanSend.Unbind(OnCanSendChanged);
        }

        protected override void OnDestroy()
        {
            if (_field == null) return;

            _field.UnregisterValueChangedCallback(OnDraftChanged);
            _field.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _sendButton.clicked -= Send;
        }

        private void OnDraftChanged(ChangeEvent<string> change)
        {
            if (ViewModel != null) ViewModel.Draft.Value = change.newValue;
        }

        private void OnDraftUpdated(string value)
        {
            _field.maxLength = Math.Max(ILobbyChatService.MaxMessageLength, value?.Length ?? 0);
            _field.SetValueWithoutNotify(value);

            if (string.IsNullOrEmpty(value) == false) return;

            ResetEmptyInputScroll();
            _field.schedule.Execute(ResetEmptyInputScroll);
        }

        private void ResetEmptyInputScroll()
        {
            if (string.IsNullOrEmpty(_field.value) == false) return;

            _field.textSelection.SelectRange(0, 0);
            ScrollView scroll = _field.Q<ScrollView>();
            if (scroll != null) scroll.scrollOffset = Vector2.zero;
        }

        private void OnCanSendChanged(bool value) => _sendButton.SetEnabled(value);

        private void Send()
        {
            if (ViewModel != null) _ = ViewModel.SendAsync();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool submit = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter;
            if (submit == false && evt.character != '\n' && evt.character != '\r') return;

            evt.StopImmediatePropagation();
            if (submit) Send();
        }

        private void OnMessagesChanged(IReadOnlyList<LobbyChatMessage> messages)
        {
            _messages.Clear();

            foreach (LobbyChatMessage message in messages)
            {
                if (message.SenderId == null)
                {
                    _messages.Add(new ChatServerMessage { Content = message.Content });
                }
                else
                {
                    _messages.Add(new ChatUserMessage { Username = message.Username, Content = message.Content });
                }
            }

            _messages.schedule.Execute(() => _messages.verticalScroller.value = _messages.verticalScroller.highValue);
        }
    }
}
