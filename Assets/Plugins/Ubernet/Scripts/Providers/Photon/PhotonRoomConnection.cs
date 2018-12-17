using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    public class PhotonRoomConnection : ConnectionBase
    {
        private const byte PhotonEventCode = 100;

        private readonly LoadBalancingClient _photonClient;
        
        private bool _sendEvents = true;

        public override IClient Server { get; protected set; } = new Client(-1);
        public override bool IsConnected => _photonClient.State == ClientState.Joined && _photonClient.IsConnectedAndReady;
        public override double ServerTime => _photonClient.loadBalancingPeer.ServerTimeInMilliSeconds / 1000.0;

        public LoadBalancingClient PhotonClient => _photonClient;
        
        public override bool SupportsHostMigration => true;

        public override bool SendEvents
        {
            get
            {
                return _sendEvents;
            }
            set
            {
                _photonClient.loadBalancingPeer.IsSendingOnlyAcks = !value;
                _sendEvents = value;
            }
        }

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
            
            RespondToPings();
        }

        public override void Update()
        {
            if (_photonClient.State != ClientState.Disconnected && SendEvents)
            {
                _photonClient.Service();
            }
        }

        /// <inheritdoc cref="Skaillz.Ubernet.IDisconnectable.Disconnect()"/>
        public override IObservable<IConnection> Disconnect()
        {
            base.Disconnect();
            
            var observable = PhotonUtils.CreateObservableForExpectedStateChange(_photonClient,
                expectedState: ClientState.ConnectedToMasterserver, returnValue: this);
            
            _photonClient.OpLeaveRoom();

            return observable;
        }

        public override void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
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
                targetClients = ResolveClientIds(target);
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

        public override void MigrateHost(int newHostId)
        {
            if (!this.IsServer(LocalClientRef))
            {
                throw new InvalidOperationException("Only the server is allowed perform host migration.");
            }
            MigrateHost(newHostId, Server.ClientId, true);
        }
        
        private void InitializeEvents()
        {
            _photonClient.OnStateChangeAction += state =>
            {
                if (state != ClientState.Joined)
                {
                    var reason = PhotonUtils.ConvertPhotonDisconnectCause(_photonClient.DisconnectedCause);
                    DisconnectedSubject.OnNext(reason);
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
                            if (newHostId != Server.ClientId)
                            {
                                MigrateHost(newHostId, Server.ClientId, false);
                            }
                        }
                        break;
                    case EventCode.PropertiesChanged:
                        var properties = (Hashtable) data.Parameters[ParameterCode.Properties];
                        if (properties.ContainsKey(GamePropertyKey.MasterClientId))
                        {
                            int newHostId = (int) properties[GamePropertyKey.MasterClientId];
                            MigrateHost(newHostId, Server.ClientId, false);
                        }
                        break;
                    case PhotonEventCode:
                        var evt = Serializer.Deserialize((byte[]) data.Parameters[ParameterCode.Data]);
                        EventSubject.OnNext(evt);
                        break;
                }
            };
        }

        private void MigrateHost(int newHostId, int oldHostId, bool broadcast)
        {
            if (ClientDict.ContainsKey(newHostId))
            {
                if (broadcast)
                {
                    var newProperties = new Hashtable {{GamePropertyKey.MasterClientId, newHostId}};
                    var expectedProperties = new Hashtable {{GamePropertyKey.MasterClientId, oldHostId}};
                    SetRoomProperties(newProperties, expectedProperties);
                }

                Server = ClientDict[newHostId];
                HostMigratedSubject.OnNext(Server);
            }
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
            LocalClientRef.ClientId = _photonClient.LocalPlayer.ID;
            Server.ClientId = _photonClient.CurrentRoom.MasterClientId;

            ClientDict = _photonClient.CurrentRoom.Players.Values
                .Select(player => (IClient) new Client(player.ID)).ToDictionary(c => c.ClientId);
        }
    }
}