using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    // TODO: use dictionary to store clients
    public class PhotonRoomConnection : IConnection
    {
        private const byte PhotonEventCode = 100;

        private readonly LoadBalancingClient _photonClient;
        // TODO: use dictionary
        private List<IClient> _clients = new List<IClient>();
        
        private readonly ISubject<DisconnectReason> _disconnectedSubject = new Subject<DisconnectReason>();
        private readonly ISubject<IClient> _playerJoinedSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _playerLeftSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _hostMigratedSubject = new Subject<IClient>();
        private readonly ISubject<NetworkEvent> _eventSubject = new Subject<NetworkEvent>();
        
        private readonly IClient _localClient = new Client(-1);
        private IClient _server = new Client(-1);
        
        public ISerializer Serializer { get; set; }
        public LoadBalancingClient PhotonClient => _photonClient;
        
        public IClient LocalClient => _localClient;
        public IClient Server => _server;
        public IReadOnlyList<IClient> Clients => _clients;

        public bool IsConnected => _photonClient.State == ClientState.Joined;
        public double ServerTime => _photonClient.loadBalancingPeer.ServerTimeInMilliSeconds / 1000.0;

        public bool SupportsHostMigration => true;

        public IObservable<DisconnectReason> OnDisconnected => _disconnectedSubject.AsObservable();
        public IObservable<IClient> OnClientJoin => _playerJoinedSubject.AsObservable();
        public IObservable<IClient> OnClientLeave => _playerLeftSubject.AsObservable();
        public IObservable<IClient> OnHostMigration => _hostMigratedSubject.AsObservable();
        public IObservable<NetworkEvent> OnEvent => _eventSubject.AsObservable();

        /// <summary>
        /// Creates a new connection to the given <see cref="LoadBalancingClient"/>.
        /// </summary>
        /// The <see cref="LoadBalancingClient"/> must be in a room.
        /// <param name="photonClient">The Photon client</param>
        /// <param name="serializer">The <see cref="ISerializer"/> used to serialize and deserialize events</param>
        public PhotonRoomConnection(LoadBalancingClient photonClient, ISerializer serializer = null)
        {
            _photonClient = photonClient;
            Serializer = serializer ?? new Serializer();
            
            InitializeEvents();
            FillRoom();
        }

       /// <inheritdoc cref="Skaillz.Ubernet.IDisconnectable.Disconnect()"/>
        public void Update()
        {
            if (_photonClient.State != ClientState.Disconnected)
            {
                _photonClient.Service();
            }
        }

        public IObservable<IConnection> Disconnect()
        {
            var observable = PhotonUtils.CreateObservableForExpectedStateChange(_photonClient,
                expectedState: ClientState.ConnectedToMasterserver, returnValue: this);
            
            _photonClient.OpLeaveRoom();

            return observable;
        }

        public IClient GetClient(int clientId)
        {
            return _clients.SingleOrDefault(client => client.ClientId == clientId);
        }

        public void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
        {
            if (_photonClient.CurrentRoom == null)
            {
                throw new InvalidOperationException("Cannot send events before joining a room.");
            }
            
            var evt = CreateEvent(code, data, target);

            ReceiverGroup? receivers = null;
            int[] targetClients = null;
            if (target == null || target == MessageTarget.Others)
            {
                receivers = ReceiverGroup.Others;
            }
            else if (target == MessageTarget.AllPlayers)
            {
                receivers = ReceiverGroup.All;
            }
            else if (target == MessageTarget.Server)
            {
                receivers = ReceiverGroup.MasterClient;
            }
            else
            {
                if (target is IClientIdResolvable)
                {
                    var resolvable = (IClientIdResolvable) target;
                    targetClients = new[] {resolvable.ClientId};
                }
                else if (target is IClientIdListResolvable)
                {
                    var resolvable = (IClientIdListResolvable) target;
                    targetClients = resolvable.GetClientIds();
                }
            }

            var options = new RaiseEventOptions
            {
                TargetActors = targetClients,
                Receivers = receivers ?? ReceiverGroup.Others
            };

            // Send the event over the network
            _photonClient.OpRaiseEvent(PhotonEventCode, evt.Data != null ? Serializer.Serialize(evt) : null,
                reliable, options);
        }

        public void MigrateHost(int newHostId)
        {
            if (!this.IsServer(_localClient))
            {
                throw new InvalidOperationException("Only the server is allowed perform host migration.");
            }
            MigrateHost(newHostId, _server.ClientId, true);
        }
        
        private void InitializeEvents()
        {
            _photonClient.OnStateChangeAction += state =>
            {
                if (state == ClientState.Disconnected)
                {
                    var reason = PhotonUtils.ConvertPhotonDisconnectCause(_photonClient.DisconnectedCause);
                    _disconnectedSubject.OnNext(reason);
                }
            };

            _photonClient.OnEventAction += data =>
            {
                switch (data.Code)
                {
                    case EventCode.Join:
                        AddClient(new Client((int) data.Parameters[ParameterCode.ActorNr]), true);
                        break;
                    case EventCode.Leave:
                        RemoveClient((int) data.Parameters[ParameterCode.ActorNr]);
                        if (data.Parameters.ContainsKey(ParameterCode.MasterClientId))
                        {
                            int newHostId = (int) data.Parameters[ParameterCode.MasterClientId];
                            if (newHostId != _server.ClientId)
                            {
                                MigrateHost(newHostId, _server.ClientId, false);
                            }
                        }
                        break;
                    case EventCode.PropertiesChanged:
                        var properties = (Hashtable) data.Parameters[ParameterCode.Properties];
                        if (properties.ContainsKey(GamePropertyKey.MasterClientId))
                        {
                            int newHostId = (int) properties[GamePropertyKey.MasterClientId];
                            MigrateHost(newHostId, _server.ClientId, false);
                        }
                        break;
                    case PhotonEventCode:
                        var evt = Serializer.Deserialize((byte[]) data.Parameters[ParameterCode.Data]);
                        _eventSubject.OnNext(evt);
                        break;
                }
            };
        }

        private void MigrateHost(int newHostId, int oldHostId, bool broadcast)
        {
            if (broadcast)
            {
                var newProperties = new Hashtable {{GamePropertyKey.MasterClientId, newHostId}};
                var expectedProperties = new Hashtable {{GamePropertyKey.MasterClientId, oldHostId}};
                SetRoomProperties(newProperties, expectedProperties);
            }

            _server = _clients.Single(c => c.ClientId == newHostId);
            _hostMigratedSubject.OnNext(_server);
        }
        
        private bool SetRoomProperties(Hashtable newProperties, Hashtable expectedProperties = null)
        {
            var roomProperties = new Dictionary<byte, object>
            {
                {ParameterCode.Properties, newProperties},
                {ParameterCode.Broadcast, true}
            };
            
            if (expectedProperties != null)
            {
                roomProperties.Add(ParameterCode.ExpectedValues, expectedProperties);
            }

            return _photonClient.loadBalancingPeer.OpCustom(OperationCode.SetProperties, roomProperties, true);
        }

        private void FillRoom()
        {
            _localClient.ClientId = _photonClient.LocalPlayer.ID;
            _server.ClientId = _photonClient.CurrentRoom.MasterClientId;

            _clients = _photonClient.CurrentRoom.Players.Values
                .Select(player => (IClient) new Client(player.ID)).ToList();
        }
        
        private void AddClient(IClient client, bool broadcast)
        {
            _clients.Add(client);

            if (broadcast)
            {
                _playerJoinedSubject.OnNext(client);
            }
        }

        private void RemoveClient(IClient client)
        {
            _clients.Remove(client);
            _playerLeftSubject.OnNext(client);
        }

        private void RemoveClient(int playerId)
        {
            var player = _clients.Find(p => p.ClientId == playerId);
            RemoveClient(player);
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