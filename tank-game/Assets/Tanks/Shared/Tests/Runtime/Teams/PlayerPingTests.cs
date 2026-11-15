using NUnit.Framework;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Tanks.Shared.Teams;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tanks.Shared.Tests.Runtime.Teams
{
    public sealed class PlayerPingTests
    {
        private NetworkManager _host;
        private NetworkManager _client;
        private GameObject _prefab;
        private TeamBehaviourService _serverTeams;

        [UnityTest]
        public IEnumerator ServerPing_ReplicatesToClientWhileTimeIsPaused()
        {
            UdpClient portReservation = new UdpClient(
                new IPEndPoint(IPAddress.Loopback, 0));

            ushort port = (ushort)((IPEndPoint)portReservation.Client
                .LocalEndPoint).Port;

            portReservation.Dispose();

            _prefab = new GameObject("Team service prefab");
            NetworkObject prefabObject = _prefab.AddComponent<NetworkObject>();

            _prefab.AddComponent<TeamBehaviourService>();

            typeof(NetworkObject).GetField(
                "GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(prefabObject, (uint)Guid.NewGuid().GetHashCode());

            _host = CreateManager("Host", port);
            _client = CreateManager("Client", port);

            Assert.That(_host.StartHost(), Is.True);
            Assert.That(_client.StartClient(), Is.True);

            yield return WaitFor(() => _client.IsConnectedClient);

            GameObject spawned = Object.Instantiate(_prefab);
            NetworkObject networkObject = spawned.GetComponent<NetworkObject>();

            _serverTeams = spawned.GetComponent<TeamBehaviourService>();

            networkObject.Spawn();

            yield return WaitFor(() => _client.SpawnManager.SpawnedObjects
                .ContainsKey(networkObject.NetworkObjectId));

            TeamBehaviourService clientTeams = _client.SpawnManager.SpawnedObjects[
                networkObject.NetworkObjectId].GetComponent<TeamBehaviourService>();

            TeamId teamId = new TeamId(1);

            _serverTeams.CreateTeamEveryoneRpc(teamId);

            yield return WaitFor(() => HasTeam(clientTeams, teamId));

            PlayerState serverPlayer = _serverTeams.GetTeam(teamId)
                .AddPlayer(_client.LocalClientId, "Remote");

            PlayerState clientPlayer = clientTeams.GetTeam(teamId)
                .AddPlayer(_client.LocalClientId, "Remote");

            PlayerState hostPlayer = _serverTeams.GetTeam(teamId)
                .AddPlayer(NetworkManager.ServerClientId, "Host");

            serverPlayer.SetPing(ulong.MaxValue);
            clientPlayer.SetPing(ulong.MaxValue);
            hostPlayer.SetPing(ulong.MaxValue);
            Time.timeScale = 0;

            yield return WaitFor(
                () => serverPlayer.PingMilliseconds != ulong.MaxValue
                && clientPlayer.PingMilliseconds == serverPlayer.PingMilliseconds);

            Assert.That(hostPlayer.PingMilliseconds, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;

            if (_client) _client.Shutdown();
            if (_host) _host.Shutdown();

            yield return null;

            if (_serverTeams) Object.Destroy(_serverTeams.gameObject);
            if (_client) Object.Destroy(_client.gameObject);
            if (_host) Object.Destroy(_host.gameObject);
            if (_prefab) Object.Destroy(_prefab);

            yield return null;
        }

        private NetworkManager CreateManager(string name, ushort port)
        {
            GameObject root = new GameObject(name);
            UnityTransport transport = root.AddComponent<UnityTransport>();
            NetworkManager manager = root.AddComponent<NetworkManager>();

            transport.SetConnectionData("127.0.0.1", port);
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ForceSamePrefabs = false
            };

            manager.AddNetworkPrefab(_prefab);
            return manager;
        }

        private static bool HasTeam(ITeamBehaviourService service, TeamId id)
        {
            foreach (Team team in service.GetAllTeams())
            {
                if (team.Id == id) return true;
            }

            return false;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 10;

            while (condition() == false)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                yield return null;
            }
        }
    }
}
