using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using UniRx;
using UnityEngine;
using UnityEngine.Profiling;

namespace Skaillz.Ubernet.Providers.LiteNetLibExperimental
{
    public class LiteNetLibConnectionExperimental : ConnectionBase
    {
        private EventBasedNetListener _listener;
        private NetManager _manager;
        private readonly List<NetPeer> _peers = new List<NetPeer>();

        private bool _isServer;

        public override IClient Server { get; protected set; } = new Client(-1);
        public override bool IsConnected => _manager != null && _manager.IsRunning;
        public override double ServerTime => 0;

        public override bool SupportsHostMigration => false;

        public override bool SendEvents { get; set; } = true;

        public static LiteNetLibConnectionExperimental CreateServer(int port, int maxConnections, string version, bool enableNatPunch)
        {
            var server = new LiteNetLibConnectionExperimental();
            server.InitAsServer(port, maxConnections, version, enableNatPunch);
            return server;
        }
        
        public static LiteNetLibConnectionExperimental CreateClient(string address, int port, int maxConnections, string version, bool enableNatPunch)
        {
            var client = new LiteNetLibConnectionExperimental();
            client.InitAsClient(address, port, maxConnections, version, enableNatPunch);
            return client;
        }

        private LiteNetLibConnectionExperimental(ISerializer serializer = null)
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
            RespondToPings();
            
            if (!_manager.Start(port))
            {
                throw new ConnectionException($"Could not initialize server on port {port}.");
            }
            
            LocalClientRef.ClientId = 1;
            Server.ClientId = 1;
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
            RespondToPings();
            
            if (!_manager.Start())
            {
                throw new ConnectionException($"Could not connect to {address}:{port}.");
            }
            
            Debug.Log("Connecting");
            _manager.Connect(address, port);
            
            // TODO: support more players
            LocalClientRef.ClientId = 2;
            Server.ClientId = 1;
            _isServer = false;

            var firstPeer = _manager.GetFirstPeer();
            if (firstPeer != null)
            {
                AddClient(new Client(_isServer ? 2 : 1), false);
            }
            Profiler.EndSample();
        }

        public override void Update()
        {
            if (SendEvents)
            {
                _manager.PollEvents();
                _manager.Flush();
            }
        }

        /// <inheritdoc cref="IDisconnectable{T}.Disconnect()"/>
        public override IObservable<IConnection> Disconnect()
        {
            base.Disconnect();
            
            return Observable.Start<IConnection>(() =>
            {
                _manager.Stop();
                return this;
            }, Scheduler.MainThread);
        }

        public override void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
        {
            var evt = CreateEvent(code, data, target);
            
            // TODO: handle targets properly
            
            int[] targetClients = ResolveClientIds(target);

            if (target == MessageTarget.AllPlayers || targetClients != null && targetClients.Contains(_isServer ? 1 : 2))
            {
                EventSubject.OnNext(evt);
            }
            
            _manager.GetPeersNonAlloc(_peers);
            if (target == MessageTarget.AllPlayers || target == MessageTarget.Others ||
                targetClients != null && targetClients.Contains(_isServer ? 2 : 1))
            {
                foreach (var peer in _peers)
                {
                    peer.Send(Serializer.Serialize(evt),
                        reliable ? SendOptions.Unreliable : SendOptions.ReliableOrdered);
                }
            }
        }

        public override void MigrateHost(int newHostId)
        {
            throw new NotImplementedException();
        }
        
        private void InitializeEvents()
        {
            _listener.NetworkReceiveEvent += (peer, reader) =>
            {
                var bytes = reader.Data;
                var evt = Serializer.Deserialize(bytes);

                EventSubject.OnNext(evt);
            };

            _listener.NetworkReceiveUnconnectedEvent += (endpoint, reader, type) =>
            {
                Debug.Log("NetworkReceiveUnconnectedEvent");
            };

            _listener.PeerConnectedEvent += peer =>
            {
                Debug.Log("Peer connected on " + _manager.LocalPort + " :" + peer.EndPoint.Host + ":" + peer.EndPoint.Port);
                var client = new Client(_isServer ? 2 : 1);
                if (!ClientDict.ContainsKey(client.ClientId))
                {
                    AddClient(client, true);
                }
            };

            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Debug.Log("Peer disconnected on " + _manager.LocalPort + " :" + peer.EndPoint.Host + ":" + peer.EndPoint.Port);
                RemoveClient(_isServer ? 2 : 1);
            };

            _listener.NetworkErrorEvent += (endpoint, code) =>
            {
                Debug.LogError("Error with code: " + code);
            };
        }
    }
}