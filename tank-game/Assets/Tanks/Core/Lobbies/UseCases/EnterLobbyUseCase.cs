using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IEnterLobbyUseCase
    {
        UniTask<ResultType<LobbyState>> HostAsync(string name, uint maxPlayers);
        UniTask<ResultType<LobbyState>> JoinAsync(LobbyId id);
        UniTask<ResultType<LobbyState>> JoinAsync(LobbyAddress address);
    }

    public sealed class EnterLobbyUseCase : IEnterLobbyUseCase
    {
        public EnterLobbyUseCase(ILobbyService service, ILobbyChatService chat)
        {
            _service = service;
            _chat = chat;
        }

        private readonly ILobbyService _service;
        private readonly ILobbyChatService _chat;

        public UniTask<ResultType<LobbyState>> HostAsync(string name, uint maxPlayers)
        {
            return EnterAsync(() => _service.HostLobbyAsync(name, maxPlayers));
        }

        public UniTask<ResultType<LobbyState>> JoinAsync(LobbyId id)
        {
            return EnterAsync(() => _service.JoinLobbyAsync(id));
        }

        public UniTask<ResultType<LobbyState>> JoinAsync(LobbyAddress address)
        {
            return EnterAsync(() => _service.JoinLobbyAsync(address));
        }

        private async UniTask<ResultType<LobbyState>> EnterAsync(Func<Task<ResultType<LobbyState>>> enter)
        {
            ResultType<LobbyState> result;

            try
            {
                result = await enter();
            }
            catch (Exception)
            {
                return Result.Failure<LobbyState>("unknown");
            }

            if (result.IsSuccess == false) return result;

            ResultType connected;

            try
            {
                connected = await _chat.ConnectAsync(result.Value.Id.Value);
            }
            catch (Exception)
            {
                connected = Result.Failure("chat-unavailable");
            }

            if (connected.IsSuccess) return result;

            ResultType disconnected;
            ResultType left;

            try
            {
                disconnected = await _chat.DisconnectAsync();
            }
            catch (Exception)
            {
                disconnected = Result.Failure("chat-unavailable");
            }

            try
            {
                left = await _service.LeaveLobbyAsync();
            }
            catch (Exception)
            {
                left = Result.Failure("failed-to-leave");
            }

            string error = left.IsSuccess == false ? left.ErrorCode
                : disconnected.IsSuccess == false ? disconnected.ErrorCode : connected.ErrorCode;

            return Result.Failure<LobbyState>(error);
        }
    }
}
