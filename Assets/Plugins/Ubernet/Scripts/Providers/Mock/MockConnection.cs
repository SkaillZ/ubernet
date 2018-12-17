using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Skaillz.Ubernet.Providers.Mock
{
    public class MockConnection : ConnectionBase
    {
        public override IClient Server
        {
            get => Network?.Server?.LocalClient ?? LocalClient;
            protected set => throw new NotImplementedException();
        }

        public override IReadOnlyList<IClient> Clients => Network != null
            ? Network.Clients.Select(c => c.LocalClient).ToList()
            : new List<IClient>(new []{ LocalClient });
        
        public MockNetwork Network { get; set; }
        public override bool IsConnected => _isConnected;
        
        public override double ServerTime => throw new NotImplementedException($"Server time is not implemented on {nameof(MockConnection)}.");

        public override bool SupportsHostMigration => true;
        public override bool SendEvents { get; set; } = true;

        private bool _isConnected = true;
        private readonly bool _actAsServer;
        private readonly Queue<NetworkEvent> _sendQueue = new Queue<NetworkEvent>();
        private readonly Queue<byte[]> _receiveQueue = new Queue<byte[]>();
        
        public MockConnection(bool actAsServer, MockNetwork network = null, ISerializer serializer = null)
        {
            _actAsServer = actAsServer;
            Serializer = serializer ?? new Serializer();
            Network = network;
            Network?.Connect(this, actAsServer);

            LocalClient.ClientId = 1;

            RespondToPings();
        }
        
        public override IObservable<IConnection> Disconnect()
        {
            base.Disconnect();
            
            _isConnected = false;
            Network?.Disconnect(this);
            _sendQueue.Clear();
            _receiveQueue.Clear();
            return Observable.Return(this);
        }

        public override void MigrateHost(int newHostId)
        {
            Network.MigrateHost(newHostId);
        }

        public override void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
        {
            var evt = new NetworkEvent
            {
                SenderId = LocalClient.ClientId,
                Code = code,
                Data = data,
                Target = target
            };
            
            _sendQueue.Enqueue(evt);
        }

        public override void Update()
        {
            if (!SendEvents)
            {
                return;
            }
            
            while (_sendQueue.Count > 0)
            {
                var evt = _sendQueue.Dequeue();
                if (Network != null)
                {
                    Network.SendEvent(evt, Serializer);
                }
                else if (evt.Target == MessageTarget.AllPlayers || evt.Target == MessageTarget.Server && _actAsServer)
                {
                    // TODO: support ID resolvables
                    _receiveQueue.Enqueue(Serializer.Serialize(evt));
                }
            }
            
            while (_receiveQueue.Count > 0)
            {
                var evt = _receiveQueue.Dequeue();
                EventSubject.OnNext(Serializer.Deserialize(evt));
            }
        }
        
        public class MockNetwork
        {
            public static readonly MockNetwork Default = new MockNetwork();
            
            public List<MockConnection> Clients { get; } = new List<MockConnection>();
            public MockConnection Server { get; private set; }

            public void Connect(MockConnection connection, bool asServer)
            {
                Clients.Add(connection);
                
                connection.LocalClient.ClientId = Math.Max(Clients.Max(c => c.LocalClient.ClientId), 1) + 1;
                if (Server == null || asServer)
                {
                    Server = connection;
                }
                
                foreach (var client in Clients)
                {
                    client.PlayerJoinedSubject.OnNext(connection.LocalClient);
                }
            }

            public void Disconnect(MockConnection connection)
            {
                Clients.Remove(connection);
                if (Server == connection)
                {
                    Server = Clients.FirstOrDefault();
                }
                
                foreach (var client in Clients)
                {
                    client.PlayerLeftSubject.OnNext(connection.LocalClient);
                }
                
                connection.DisconnectedSubject.OnNext(DisconnectReason.CleanDisconnect);
            }

            public void SendEvent(NetworkEvent evt, ISerializer serializer)
            {
                var target = evt.Target;
                IEnumerable<MockConnection> targetConnections;
                
                if (target == null || target == MessageTarget.Others)
                {
                    targetConnections = Clients.Where(c => c.LocalClient.ClientId != evt.SenderId);
                }
                else if (target == MessageTarget.AllPlayers)
                {
                    targetConnections = Clients;
                }
                else if (target == MessageTarget.Server)
                {
                    targetConnections = new[] { Server };
                }
                else
                {
                    int[] targetClients = null;
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

                    // ReSharper disable once AssignNullToNotNullAttribute
                    targetConnections = Clients.Where(c => targetClients.Contains(c.LocalClient.ClientId));
                }

                foreach (var connection in targetConnections)
                {
                    connection._receiveQueue.Enqueue(serializer.Serialize(evt));
                }
            }

            public void MigrateHost(int newHostId)
            {
                Server = Clients.First(c => c.LocalClient.ClientId == newHostId);

                foreach (var mockConnection in Clients)
                {
                    mockConnection.HostMigratedSubject.OnNext(Server.LocalClient);
                }
            }
        }
    }
}