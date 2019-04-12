using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UniRx;

namespace Skaillz.Ubernet
{
    public abstract class ConnectionBase : IConnection
    {
        public const float PingTimeout = 10f;
        
        protected Dictionary<int, IClient> ClientDict = new Dictionary<int, IClient>();
        protected readonly ISubject<DisconnectReason> DisconnectedSubject = new Subject<DisconnectReason>();
        protected readonly ISubject<IClient> PlayerJoinedSubject = new Subject<IClient>();
        protected readonly ISubject<IClient> PlayerLeftSubject = new Subject<IClient>();
        protected readonly ISubject<IClient> HostMigratedSubject = new Subject<IClient>();
        protected readonly ISubject<NetworkEvent> EventSubject = new Subject<NetworkEvent>();
        protected readonly IClient LocalClientRef = new Client(-1);
        
        public ISerializer Serializer { get; set; }
        public IClient LocalClient => LocalClientRef;
        public abstract IClient Server { get; protected set; }
        
        public virtual IReadOnlyList<IClient> Clients => ClientDict.Values.ToList();
        public abstract bool IsConnected { get; }
        public abstract double ServerTime { get; }
        public long CurrentRoundTripTime => _currentRoundTripTime;

        public float AutoPingInterval
        {
            get { return _autoPingInterval; }
            set
            {
                _autoPingInterval = value;
                RegisterAutoPing();
            }
        }

        public abstract bool SupportsHostMigration { get; }
        public abstract bool SendEvents { get; set; }
        
        public IObservable<DisconnectReason> OnDisconnected => DisconnectedSubject.AsObservable();
        public IObservable<IClient> OnClientJoin => PlayerJoinedSubject.AsObservable();
        public IObservable<IClient> OnClientLeave => PlayerLeftSubject.AsObservable();
        public IObservable<IClient> OnHostMigration => HostMigratedSubject.AsObservable();
        public IObservable<NetworkEvent> OnEvent => EventSubject.AsObservable();

        public IObservable<Exception> OnAutoPingError => _autoPingErrorSubject.AsObservable();

        private readonly Subject<Exception> _autoPingErrorSubject = new Subject<Exception>();
        private IDisposable _autoPingTimingSubscription;
        private IDisposable _pingSubscription;
        private long _currentRoundTripTime;
        private float _autoPingInterval = 1f;
        
        // Avoids GC allocations by using cached arrays
        private int[] EmptyClientIdList = new int[0];
        private int[] OneClientIdList = new int[1];

        public abstract void MigrateHost(int newHostId);

        public IClient GetClient(int clientId)
        {
            if (ClientDict.ContainsKey(clientId))
            {
                return ClientDict[clientId];
            }

            return null;
        }

        public abstract void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true);

        public virtual IObservable<long> Ping(IClient client)
        {
            return Observable.Create<long>(observer =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                
                OnEvent.First(e => e.SenderId == client.ClientId && e.Code == DefaultEvents.Pong)
                    .Timeout(TimeSpan.FromSeconds(PingTimeout))
                    .Select(e =>
                    {
                        stopWatch.Stop();
                        return stopWatch.ElapsedMilliseconds;
                    })
                    .Subscribe(time =>
                    {
                        observer.OnNext(time);
                        observer.OnCompleted();
                    });
                
                // BUG: passing null currently leads to an error in PhotonRoomConnection
                SendEvent(DefaultEvents.Ping, (byte) 0, client);
                
                return Disposable.Create(observer.OnCompleted);
            });
        }
        
        public virtual IObservable<long> PingServer()
        {
            return Ping(Server).Do(rtt => _currentRoundTripTime = rtt);
        }

        protected void AddClient(IClient client, bool broadcast)
        {
            ClientDict.Add(client.ClientId, client);

            if (broadcast)
            {
                PlayerJoinedSubject.OnNext(client);
            }
        }

        private void RemoveClient(IClient client)
        {
            RemoveClient(client.ClientId);
        }

        protected void RemoveClient(int clientId)
        {
            if (ClientDict.ContainsKey(clientId))
            {
                var client = ClientDict[clientId];
                PlayerLeftSubject.OnNext(client);
                ClientDict.Remove(clientId);
            }
        }

        protected void RespondToPings()
        {
            _pingSubscription = OnEvent.Where(e => e.Code == DefaultEvents.Ping)
                .Subscribe(e =>
                {
                    SendEvent(DefaultEvents.Pong, (byte) 0, new Client(e.SenderId));
                });
            
            RegisterAutoPing();
        }

        protected void RegisterAutoPing()
        {
            
            if (_autoPingTimingSubscription != null)
            {
                _autoPingTimingSubscription.Dispose();
                _autoPingTimingSubscription = null;
            }

            if (_autoPingInterval > 0f)
            {
                _autoPingTimingSubscription = Observable.Interval(TimeSpan.FromSeconds(_autoPingInterval))
                    .Subscribe(_ => PingServer().Subscribe(), err => _autoPingErrorSubject.OnNext(err));
            }
        }

        protected NetworkEvent CreateEvent(byte code, object data, IMessageTarget target)
        {
            return new NetworkEvent
            {
                SenderId = LocalClientRef.ClientId,
                Code = code,
                Data = data,
                Target = target
            };
        }

        protected int[] ResolveClientIds(IMessageTarget target)
        {
            if (target is IClientIdResolvable)
            {
                var resolvable = (IClientIdResolvable) target;
                OneClientIdList[0] = resolvable.ClientId;
                return OneClientIdList;
            }
            if (target is IClientIdListResolvable)
            {
                var resolvable = (IClientIdListResolvable) target;
                return resolvable.GetClientIds();
            }

            return EmptyClientIdList;
        }

        public virtual IObservable<IConnection> Disconnect()
        {
            _pingSubscription?.Dispose();
            _autoPingTimingSubscription?.Dispose();
            return Observable.Return(this);
        }
        
        public abstract void Update();
    }
}