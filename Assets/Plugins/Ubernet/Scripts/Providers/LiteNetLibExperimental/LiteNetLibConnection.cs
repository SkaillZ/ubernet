using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using UniRx;
using UnityEngine;
using UnityEngine.Profiling;

namespace Skaillz.Ubernet.Providers.LiteNetLibExperimental
{
    public class LiteNetLibConnection : IConnection
    {
        private readonly Dictionary<int, IClient> _clients = new Dictionary<int, IClient>();
        
        private readonly ISubject<DisconnectReason> _disconnectedSubject = new Subject<DisconnectReason>();
        private readonly ISubject<IClient> _playerJoinedSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _playerLeftSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _hostMigratedSubject = new Subject<IClient>();
        private readonly ISubject<NetworkEvent> _eventSubject = new Subject<NetworkEvent>();
        
        private readonly IClient _localClient = new Client(-1);
        private readonly IClient _server = new Client(-1);

        private EventBasedNetListener _listener;
        private NetManager _manager;
        private readonly List<NetPeer> _peers = new List<NetPeer>();

        private bool _isServer;

        public ISerializer Serializer { get; set; }
        
        public IClient LocalClient => _localClient;
        public IClient Server => _server;
        public IReadOnlyList<IClient> Clients => _clients.Values.ToList();

        public bool IsConnected => _manager != null && _manager.IsRunning;
        public double ServerTime => 0;

        public bool SupportsHostMigration => false;

        public bool SendEvents { get; set; } = true;

        public IObservable<DisconnectReason> OnDisconnected => _disconnectedSubject.AsObservable();
        public IObservable<IClient> OnClientJoin => _playerJoinedSubject.AsObservable();
        public IObservable<IClient> OnClientLeave => _playerLeftSubject.AsObservable();
        public IObservable<IClient> OnHostMigration => _hostMigratedSubject.AsObservable();
        public IObservable<NetworkEvent> OnEvent => _eventSubject.AsObservable();

        public static LiteNetLibConnection CreateServer(int port, int maxConnections, string version, bool enableNatPunch)
        {
            var server = new LiteNetLibConnection();
            server.InitAsServer(port, maxConnections, version, enableNatPunch);
            return server;
        }
        
        public static LiteNetLibConnection CreateClient(string address, int port, int maxConnections, string version, bool enableNatPunch)
        {
            var client = new LiteNetLibConnection();
            client.InitAsClient(address, port, maxConnections, version, enableNatPunch);
            return client;
        }

        private LiteNetLibConnection(ISerializer serializer = null)
        {
            Serializer = serializer ?? new Serializer();
        }

        private void InitAsServer(int port, int maxConnections, string version, bool enableNatPunch)
        {
            Debug.Log("InitAsServer");
            Profiler.BeginSample("InitAsServer");
            _listener = new EventBasedNetListener();
            
            _manager = new NetManager(_listener, maxConnections, version);
            _manager.NatPunchEnabled = enableNatPunch;
            _manager.UpdateTime = 0;
            InitializeEvents();
            
            if (!_manager.Start(port))
            {
                throw new ConnectionException($"Could not initialize server on port {port}.");
            }
            
            _localClient.ClientId = 1;
            _server.ClientId = 1;
            _isServer = true;
            
            Profiler.EndSample();
        }

        private void InitAsClient(string address, int port, int maxConnections, string version, bool enableNatPunch)
        {
            Debug.Log("InitAsClient");
            Profiler.BeginSample("InitAsClient");
            _listener = new EventBasedNetListener();
            
            _manager = new NetManager(_listener, maxConnections, version);
            _manager.NatPunchEnabled = enableNatPunch;
            _manager.UpdateTime = 0;
            InitializeEvents();
            
            if (!_manager.Start())
            {
                throw new ConnectionException($"Could not connect to {address}:{port}.");
            }
            
            Debug.Log("Connecting");
            _manager.Connect(address, port);
            
            // TODO: support more players
            _localClient.ClientId = 2;
            _server.ClientId = 1;
            _isServer = false;

            var firstPeer = _manager.GetFirstPeer();
            if (firstPeer != null)
            {
                AddClient(new Client(_isServer ? 2 : 1), false);
            }
            Profiler.EndSample();
        }

        public void Update()
        {
            if (SendEvents)
            {
                _manager.PollEvents();
                _manager.Flush();
            }
        }

        /// <inheritdoc cref="IDisconnectable{T}.Disconnect()"/>
        public IObservable<IConnection> Disconnect()
        {
            return Observable.Start<IConnection>(() =>
            {
                _manager.Stop();
                return this;
            }, Scheduler.MainThread);
        }

        public IClient GetClient(int clientId)
        {
            if (!_clients.ContainsKey(clientId))
            {
                return null;
            }
            return _clients[clientId];
        }

        public void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
        {
            var evt = CreateEvent(code, data, target);
            
            Debug.Log("Send event with code " + code + "(" + _manager.PeersCount + " peers). this is " + _manager.LocalPort);
            // TODO: handle targets

            if (target == MessageTarget.AllPlayers)
            {
                _eventSubject.OnNext(evt);
            }
            
            _manager.GetPeersNonAlloc(_peers);
            foreach (var peer in _peers)
            {
                peer.Send(Serializer.Serialize(evt), reliable ? SendOptions.Unreliable : SendOptions.ReliableOrdered);
            }
        }

        public void MigrateHost(int newHostId)
        {
            throw new NotImplementedException();
        }
        
        private void InitializeEvents()
        {
            _listener.NetworkReceiveEvent += (peer, reader) =>
            {
                Debug.Log("NetworkReceiveEvent on " + _manager.LocalPort + " by peer: " + peer.EndPoint.Host + ":" + peer.EndPoint.Port);
                var bytes = reader.Data;
                var evt = Serializer.Deserialize(bytes);

                Debug.Log("Received event with code " + evt.Code);
                if (evt.Data is string)
                {
                    Debug.Log("data: " + evt.Data);
                }
                _eventSubject.OnNext(evt);
            };

            _listener.NetworkReceiveUnconnectedEvent += (endpoint, reader, type) =>
            {
                Debug.Log("NetworkReceiveUnconnectedEvent");
            };

            _listener.PeerConnectedEvent += peer =>
            {
                Debug.Log("Peer connected on " + _manager.LocalPort + " :" + peer.EndPoint.Host + ":" + peer.EndPoint.Port);
                AddClient(new Client(_isServer ? 2 : 1), true);
            };

            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Debug.Log("Peer disonnected on " + _manager.LocalPort + " :" + peer.EndPoint.Host + ":" + peer.EndPoint.Port);
                RemoveClient(_isServer ? 2 : 1);
            };

            _listener.NetworkErrorEvent += (endpoint, code) =>
            {
                Debug.LogError("Error with code: " + code);
            };
        }
        
        private void AddClient(IClient client, bool broadcast)
        {
            _clients[client.ClientId] = client;

            if (broadcast)
            {
                _playerJoinedSubject.OnNext(client);
            }
        }

        private void RemoveClient(IClient client)
        {
            _clients.Remove(client.ClientId);
            _playerLeftSubject.OnNext(client);
        }

        private void RemoveClient(int playerId)
        {
            if (_clients.ContainsKey(playerId))
            {
                var player = _clients[playerId];
                RemoveClient(player);
            }
        }
        
        private NetworkEvent CreateEvent(byte code, object data, IMessageTarget target)
        {
            return new NetworkEvent
            {
                SenderId = _localClient.ClientId,
                Code = code,
                Data = data,
                Target = target
            };
        }
    }
}