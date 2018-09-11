using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Skaillz.Ubernet.Providers.Mock
{
    public class MockConnection : IConnection
    {
        private readonly ISubject<DisconnectReason> _disconnectedSubject = new Subject<DisconnectReason>();
        private readonly ISubject<IClient> _playerJoinedSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _playerLeftSubject = new Subject<IClient>();
        private readonly ISubject<IClient> _hostMigratedSubject = new Subject<IClient>();
        private readonly ISubject<NetworkEvent> _eventSubject = new Subject<NetworkEvent>();
        
        public ISerializer Serializer { get; set; }
        
        public IClient LocalClient { get; private set; } = new Client(-1);

        public IClient Server => Network?.Server?.LocalClient ?? LocalClient;

        public IReadOnlyList<IClient> Clients => Network != null
            ? Network.Clients.Select(c => c.LocalClient).ToList()
            : new List<IClient>(new []{ LocalClient });
        
        public MockNetwork Network { get; set; }
        public bool IsConnected { get; private set; }
        
        public double ServerTime => throw new NotImplementedException($"Server time is not implemented on {nameof(MockConnection)}.");

        public bool SupportsHostMigration => true;

        public IObservable<DisconnectReason> OnDisconnected => _disconnectedSubject.AsObservable();
        public IObservable<IClient> OnClientJoin => _playerJoinedSubject.AsObservable();
        public IObservable<IClient> OnClientLeave => _playerLeftSubject.AsObservable();
        public IObservable<IClient> OnHostMigration => _hostMigratedSubject.AsObservable();
        public IObservable<NetworkEvent> OnEvent => _eventSubject.AsObservable();
        
        private readonly Queue<NetworkEvent> _sendQueue = new Queue<NetworkEvent>();
        private readonly Queue<byte[]> _receiveQueue = new Queue<byte[]>();

        public MockConnection(ISerializer serializer, MockNetwork network = null, bool actAsServer = false)
            : this(network, actAsServer)
        {
            Serializer = serializer;
        }
        
        public MockConnection(MockNetwork network = null, bool actAsServer = false)
        {
            Serializer = Serializer ?? new Serializer();
            Network = network;
            Network?.Connect(this, actAsServer);
        }
        
        public IObservable<IConnection> Disconnect()
        {
            IsConnected = false;
            Network?.Disconnect(this);
            _sendQueue.Clear();
            _receiveQueue.Clear();
            return Observable.Return(this);
        }

        public void MigrateHost(int newHostId)
        {
            Network.MigrateHost(newHostId);
        }

        public IClient GetClient(int clientId)
        {
            if (LocalClient.ClientId == clientId)
            {
                return LocalClient;
            }

            return Network?.Clients?.FirstOrDefault(c => c.LocalClient.ClientId == clientId)?.LocalClient;
        }

        public void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true)
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
        
        public void Update()
        {
            while (_sendQueue.Count > 0)
            {
                var evt = _sendQueue.Dequeue();
                if (Network != null)
                {
                    Network.SendEvent(evt, Serializer);
                }
                else if (evt.Target == MessageTarget.AllPlayers)
                {
                    // TODO: support ID resolvables
                    _receiveQueue.Enqueue(Serializer.Serialize(evt));
                }
            }
            
            while (_receiveQueue.Count > 0)
            {
                var evt = _receiveQueue.Dequeue();
                _eventSubject.OnNext(Serializer.Deserialize(evt));
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
                
                connection.LocalClient.ClientId = Math.Max(Clients.Max(c => c.LocalClient.ClientId), 0) + 1;
                if (Server == null || asServer)
                {
                    Server = connection;
                }
                
                foreach (var client in Clients)
                {
                    client._playerJoinedSubject.OnNext(connection.LocalClient);
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
                    client._playerLeftSubject.OnNext(connection.LocalClient);
                }
                
                connection._disconnectedSubject.OnNext(DisconnectReason.CleanDisconnect);
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
                    mockConnection._hostMigratedSubject.OnNext(Server.LocalClient);
                }
            }
        }
    }
}