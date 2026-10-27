using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Unity.Services.Vivox;
using UnityEngine;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class VivoxLobbyChatService : ILobbyChatService, IDisposable
    {
        public VivoxLobbyChatService(IAuthService auth, ILobbyService lobbies)
        {
            _auth = auth;
            _lobbies = lobbies;
            Messages = _messages.AsReadOnly();

            _lobbies.CurrentLobbyState.Id.Bind(OnLobbyChanged);
            Application.quitting += Dispose;
        }

        public event Action<LobbyChatMessage> MessageReceived;

        public IReadOnlyList<LobbyChatMessage> Messages { get; }

        private readonly IAuthService _auth;
        private readonly ILobbyService _lobbies;

        private readonly SemaphoreSlim _operations = new(1, 1);
        private readonly List<LobbyChatMessage> _pending = new();
        private readonly HashSet<string> _received = new();
        private readonly List<LobbyChatMessage> _messages = new();

        private readonly CancellationTokenSource _lifetime = new();
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _loginTimeout = TimeSpan.FromSeconds(90);
        private readonly TimeSpan _loginRetryDelay = TimeSpan.FromSeconds(10);

        private IVivoxService _service;
        private LobbyId _lobbyId;
        private string _channel;
        private bool _loadingHistory;
        private bool _stopping;

        public void Dispose()
        {
            if (_stopping) return;

            _stopping = true;
            Application.quitting -= Dispose;
            _lobbies.CurrentLobbyState.Id.Unbind(OnLobbyChanged);
            _lifetime.Cancel();

            if (_service == null) return;

            _service.ChannelMessageReceived -= OnMessageReceived;
            if (_service.InitializationState == VivoxInitializationState.Initialized) _service.Uninitialize();
        }

        private void OnLobbyChanged(LobbyId id)
        {
            if (string.IsNullOrEmpty(id.Value)) _ = DisconnectAsync();
        }

        public async Task<ResultType> ConnectAsync(LobbyId lobbyId)
        {
            await _operations.WaitAsync();

            try
            {
                if (_stopping) return Result.Failure("cancelled");
                if (string.IsNullOrEmpty(lobbyId.Value)) return Result.Failure("chat-unavailable");
                if (_channel != null && _lobbyId.Value == lobbyId.Value &&
                    _service.IsLoggedIn && _service.ActiveChannels.ContainsKey(_channel)) return Result.Success();

                ResultType disconnected = await LeaveChannelAsync();
                if (disconnected.IsSuccess == false) return disconnected;

                _service ??= VivoxService.Instance;
                await WaitForOperationAsync(_service.InitializeAsync(), "initialization");

                if (_service.IsLoggedIn == false)
                {
                    using var loginCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);

                    try
                    {
                        await WaitForOperationAsync(LoginAsync(loginCancellation.Token), "login", _loginTimeout);
                    }
                    finally
                    {
                        loginCancellation.Cancel();
                    }
                }

                _lobbyId = lobbyId;
                _channel = "lobby-" + lobbyId.Value;
                _loadingHistory = true;
                _service.ChannelMessageReceived += OnMessageReceived;

                await WaitForOperationAsync(_service.JoinGroupChannelAsync(_channel, ChatCapability.TextOnly), "channel join");
                var historyRequest = _service.GetChannelTextMessageHistoryAsync(_channel, 50);
                await WaitForOperationAsync(historyRequest, "history loading");
                var history = await historyRequest;
                _pending.AddRange(history.Select(message => MapMessage(message, true)));

                foreach (LobbyChatMessage message in _pending.OrderBy(x => x.ReceivedTime).ToArray())
                {
                    Publish(message);
                }

                _pending.Clear();
                _loadingHistory = false;
                return Result.Success();
            }
            catch (Exception ex)
            {
                if (_stopping) return Result.Failure("cancelled");

                Debug.LogWarning($"Lobby chat connection failed: {ex.Message}");

                if (ex is TimeoutException && _service.InitializationState == VivoxInitializationState.Initialized)
                {
                    _service.Uninitialize();
                }

                await LeaveChannelAsync();
                return Result.Failure("chat-unavailable");
            }
            finally
            {
                _operations.Release();
            }
        }

        public async Task<ResultType> SendAsync(LobbyId lobbyId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > ILobbyChatService.MaxMessageLength)
            {
                return Result.Failure("chat-invalid-message");
            }

            await _operations.WaitAsync();

            try
            {
                if (_stopping) return Result.Failure("cancelled");
                if (_channel == null || _lobbyId.Value != lobbyId.Value ||
                    _service.IsLoggedIn == false || _service.ActiveChannels.ContainsKey(_channel) == false)
                {
                    return Result.Failure("chat-unavailable");
                }

                await WaitForOperationAsync(_service.SendChannelTextMessageAsync(_channel, message.Trim()), "message sending");
                return Result.Success();
            }
            catch (Exception ex)
            {
                if (_stopping) return Result.Failure("cancelled");
                Debug.LogWarning($"Lobby chat send failed: {ex.Message}");
                return Result.Failure("chat-send-failed");
            }
            finally
            {
                _operations.Release();
            }
        }

        public async Task<ResultType> DisconnectAsync()
        {
            await _operations.WaitAsync();

            try
            {
                return await LeaveChannelAsync();
            }
            finally
            {
                _operations.Release();
            }
        }

        private async Task<ResultType> LeaveChannelAsync()
        {
            string channel = _channel;
            _channel = null;
            _lobbyId = default;
            _loadingHistory = false;
            _pending.Clear();
            _received.Clear();
            _messages.Clear();

            if (_service == null) return Result.Success();
            _service.ChannelMessageReceived -= OnMessageReceived;

            try
            {
                if (_stopping == false && channel != null && _service.ActiveChannels.ContainsKey(channel))
                {
                    await WaitForOperationAsync(_service.LeaveChannelAsync(channel), "channel leaving");
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                if (_stopping) return Result.Success();
                Debug.LogWarning($"Lobby chat disconnect failed: {ex.Message}");
                _channel = channel;
                return Result.Failure("chat-unavailable");
            }
        }

        private async Task LoginAsync(CancellationToken cancellation)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                cancellation.ThrowIfCancellationRequested();
                if (_service.IsLoggedIn) return;

                try
                {
                    await _service.LoginAsync(new LoginOptions
                    {
                        DisplayName = _auth.CurrentProfile.DisplayName.Value
                    });
                }
                catch (Exception ex) when (ex is TimeoutException ||
                    ex is VivoxApiException { StatusCode: 5044 })
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (_service.IsLoggedIn) return;
                    if (attempt != 0) throw;

                    Debug.Log($"Vivox login handshake failed: {ex.Message}. Retrying once using the existing connector.");
                    await Task.Delay(_loginRetryDelay, cancellation);
                    continue;
                }

                cancellation.ThrowIfCancellationRequested();

                if (_service.IsLoggedIn == false)
                {
                    throw new InvalidOperationException("Vivox login finished without a signed-in session.");
                }

                return;
            }
        }

        private async Task WaitForOperationAsync(Task operation, string stage, TimeSpan? operationTimeout = null)
        {
            TimeSpan duration = operationTimeout ?? _operationTimeout;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            Task deadline = Task.Delay(duration, timeout.Token);

            if (await Task.WhenAny(operation, deadline) == operation)
            {
                timeout.Cancel();
                await operation;
                return;
            }

            _ = ObserveOperationAsync(operation);
            _lifetime.Token.ThrowIfCancellationRequested();
            if (_service.InitializationState == VivoxInitializationState.Initialized) _service.Uninitialize();
            throw new TimeoutException($"Vivox {stage} timed out after {duration.TotalSeconds:0} seconds.");
        }

        private async Task ObserveOperationAsync(Task operation)
        {
            try { await operation; }
            catch (Exception) { }

            if (_stopping && _service.InitializationState == VivoxInitializationState.Initialized)
            {
                _service.Uninitialize();
            }
        }

        private LobbyChatMessage MapMessage(VivoxMessage message, bool isHistory = false)
        {
            return new LobbyChatMessage
            {
                LobbyId = _lobbyId,
                Id = message.MessageId,
                SenderId = message.SenderPlayerId,
                Username = string.IsNullOrEmpty(message.SenderDisplayName)
                    ? message.SenderPlayerId : message.SenderDisplayName,
                Content = message.MessageText,
                ReceivedTime = message.ReceivedTime,
                IsHistory = isHistory
            };
        }

        private void OnMessageReceived(VivoxMessage message)
        {
            if (_channel == null || message.ChannelName != _channel) return;

            LobbyChatMessage mapped = MapMessage(message);
            if (_loadingHistory) _pending.Add(mapped);
            else Publish(mapped);
        }

        private void Publish(LobbyChatMessage message)
        {
            if (string.IsNullOrEmpty(message.Id) || _received.Add(message.Id) == false) return;

            _messages.Add(message);
            MessageReceived?.Invoke(message);
        }
    }
}
