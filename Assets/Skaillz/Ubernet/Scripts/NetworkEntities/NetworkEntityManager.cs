using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UniRx;

namespace Skaillz.Ubernet.NetworkEntities
{
    public class NetworkEntityManager
    {
        /// <summary>
        /// The times per second Entity and Player updates are sent by default
        /// </summary>
        public const int DefaultSerializationRate = 20;
        public const int MinSafeEntityId = 1000000;
        private const double PlayerListTimeout = 5000.0;

        private readonly SerializationHelper _helper = new SerializationHelper();
        private readonly Dictionary<int, INetworkEntity> _entities = new Dictionary<int, INetworkEntity>();
        private readonly Dictionary<int, IPlayer> _players = new Dictionary<int, IPlayer>();
        private IConnection _connection;
        private IPlayer _localPlayer;
        private byte[] _localPlayerCache;

        private int _nextEntityId = 1;
        private IDisposable _eventSubscription;
        private IDisposable _playerBroadcastSubscription;

        private readonly ISubject<INetworkEntity> _entityCreatedSubject = new Subject<INetworkEntity>();
        private readonly ISubject<INetworkEntity> _entityDestroyedSubject = new Subject<INetworkEntity>();
        private readonly ISubject<INetworkEntity> _entityUpdatedSubject = new Subject<INetworkEntity>();
        
        private readonly ISubject<IPlayer> _playerJoinedSubject = new Subject<IPlayer>();
        private readonly ISubject<IPlayer> _playerLeftSubject = new Subject<IPlayer>();
        private readonly ISubject<IPlayer> _playerUpdatedSubject = new Subject<IPlayer>();

        /// <summary>
        /// The <see cref="IConnection"/> used by the entity manager
        /// </summary>
        public virtual IConnection Connection
        {
            get => _connection;
            set
            {
                _eventSubscription?.Dispose();
                _playerBroadcastSubscription?.Dispose();
                _connection = value;
                RegisterEvents();

                if (_localPlayer != null)
                {
                    RefreshPlayerClientId();
                }
            }
        }

        /// <summary>
        /// If activated (default), player objects will be destroyed automatically after their clients disconnected
        /// </summary>
        public virtual bool DestroyPlayersAfterLeave { get; set; } = true;
        
        /// <summary>
        /// If activated, entities will be destroyed automatically when. the player owning them leaves the game.
        /// </summary>
        /// This option is deactivated by default.
        public virtual bool DestroyPlayerEntitiesAfterLeave { get; set; }

        /// <summary>
        /// The player object representing the local player
        /// </summary>
        public virtual IPlayer LocalPlayer => _localPlayer;
        
        /// <summary>
        /// A read-only list containing all connected players
        /// </summary>
        public virtual IReadOnlyList<IPlayer> Players => _players.Values.ToList();
        
        /// <summary>
        /// A read-only list of all active entities
        /// </summary>
        public virtual IReadOnlyList<INetworkEntity> Entities => _entities.Values.ToList();

        /// <summary>
        /// Called when an entity is created
        /// </summary>
        public virtual IObservable<INetworkEntity> OnEntityCreated => _entityCreatedSubject.AsObservable();
        
        /// <summary>
        /// Called when an entity is destroyed
        /// </summary>
        public virtual IObservable<INetworkEntity> OnEntityDestroyed => _entityDestroyedSubject.AsObservable();
        
        /// <summary>
        /// Called when an entity gets updated by a remote player
        /// </summary>
        public virtual IObservable<INetworkEntity> OnEntityUpdated => _entityUpdatedSubject.AsObservable();
        
        /// <summary>
        /// Called when a player joins the game
        /// </summary>
        public virtual IObservable<IPlayer> OnPlayerJoined => _playerJoinedSubject.AsObservable();
        
        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        public virtual IObservable<IPlayer> OnPlayerLeft => _playerLeftSubject.AsObservable();
        
        /// <summary>
        /// Called when a player's properties are updated
        /// </summary>
        public virtual IObservable<IPlayer> OnPlayerUpdated => _playerUpdatedSubject.AsObservable();

        /// <summary>
        /// Creates a new <see cref="NetworkEntityManager"/> from the given <see cref="IConnection"/>
        /// </summary>
        /// <param name="connection">The connection to create an entity manager for</param>
        /// <param name="serializationRate">The rate at which Entity and player Updates are sent per second</param>
        /// <returns>An entity manager for the given connection</returns>
        public static NetworkEntityManager Create([NotNull] IConnection connection, 
            int serializationRate = DefaultSerializationRate)
        {
            var manager = new NetworkEntityManager(connection);

            Observable.Interval(TimeSpan.FromMilliseconds(1f / serializationRate))
                .TakeWhile(_ => manager._connection.IsConnected)
                .Subscribe(_ =>
                {
                    manager.Update();
                });

            return manager;
        }
        
        internal NetworkEntityManager()
        {
        }

        public NetworkEntityManager([NotNull] IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection), "Connection must not be null");
            }
            
            _connection = connection;
            RegisterEvents();
        }

        /// <summary>
        /// Initializes an entity manager.
        /// </summary>
        /// When initialization is finished, the <see cref="Players"/> list is already populated
        /// and other players have been informed about the newly joined client.
        /// 
        /// <returns>An observable that resolves with the initialized instance</returns>
        /// <exception cref="PlayerNotSetException">If <see cref="SetLocalPlayer"/> was not called before</exception>
        public virtual IObservable<NetworkEntityManager> Initialize()
        {
            // TODO: throw if already initialized
            if (_localPlayer == null)
            {
                throw new PlayerNotSetException($"Call {nameof(SetLocalPlayer)} before calling {nameof(Initialize)}.");
            }
            
            return WaitForPlayerList();
        }

        /// <summary>
        /// Creates the entity on all clients.
        /// </summary>
        /// Registers an entity and sends the creation event to all other clients.
        /// <param name="entity">The entity that should be instantiated</param>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        /// <exception cref="OverflowException">If the entity manager has run out of usable IDs since too many entities
        /// have been created</exception>
        public virtual void InstantiateEntity([NotNull] INetworkEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity must not be null.");
            }
            
            if (_nextEntityId + 1 >= MinSafeEntityId)
            {
                // Try to find an unused safe ID
                bool foundSafeId = false;
                for (_nextEntityId = 1; _nextEntityId < MinSafeEntityId; _nextEntityId++)
                {
                    if (!_entities.ContainsKey(_nextEntityId))
                    {
                        foundSafeId = true;
                        break;
                    }
                }

                if (!foundSafeId)
                {
                    throw new OverflowException($"Too many entities have been created. The maximum number of " +
                                                $"entities that can be automatically created is {MinSafeEntityId - 1}.");
                }
            }

            entity.Id = _nextEntityId++;
            
            if (_entities.ContainsKey(entity.Id))
            {
                throw new InvalidOperationException($"An entity with ID {entity.Id} has already been registered.");
            }

            RegisterEntity(entity);
            BroadcastEntityCreation(entity);
        }

        /// <summary>
        /// Destroys the entity with the given ID on all clients.
        /// </summary>
        /// Unregisters the given entity and sends its destroy event to all other clients.
        /// <param name="id">The ID of the entity to destroy</param>
        public virtual void DestroyEntity(int id)
        {
            UnregisterEntity(id, true);
            _connection?.SendEvent(DefaultEvents.NetworkEntityDestroy, id);
        }
        
        
        /// <summary>
        /// Sets the player object of the local client.
        /// </summary>
        /// Must be called before calling <see cref="Initialize"/>.
        /// 
        /// <param name="player">The <see cref="IPlayer"/> object that should be set as the local player.</param>
        /// <returns>The <see cref="NetworkEntityManager"/> instance</returns>
        /// <exception cref="ArgumentNullException">If the <see cref="player"/> is null</exception>
        public virtual NetworkEntityManager SetLocalPlayer([NotNull] IPlayer player)
        {
            // TODO: throw exception if already initialized
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player), "Player must not be null.");
            }
            
            player.ClientId = _connection.LocalClient.ClientId;
            _localPlayer = player;
            
            RegisterLocalPlayer();
            return this;
        }

        /// <summary>
        /// Returns the local or remote player with the given ID
        /// </summary>
        /// <param name="clientId">The ID of the player to find</param>
        /// <returns>The player that matches the ID or null if no player matches</returns>
        public virtual IPlayer GetPlayer(int clientId)
        {
            if (!_players.ContainsKey(clientId))
            {
                return null;
            }
            return _players[clientId];
        }

        /// <summary>
        /// Sends all entity and player updates to the <see cref="Connection"/>.
        /// </summary>
        ///
        /// This does not send any updates over the network, since they will only be queued for sending.
        /// Call <see cref="IConnection.Update"/> on the connection to send the queued events.
        /// If you create an <see cref="NetworkEntityManager"/> with <see cref="Create"/>, <see cref="Update"/> is called
        /// automatically.
        /// 
        public virtual void Update()
        {
            SendEntityAndPlayerUpdates();
        }

        /// <summary>
        /// Registers an entity without sending the creation event to the other players.
        /// </summary>
        /// Use this instead of <see cref="InstantiateEntity"/> to register an entity that exists on all clients from the start.
        /// 
        /// <param name="entity"></param>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        /// <exception cref="ArgumentException">If the entity's ID is negative</exception>
        public void RegisterEntity([NotNull] INetworkEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity must not be null.");
            }
            
            if (entity.Id < 0)
            {
                throw new ArgumentException("Tried to register an entity with a negative ID.", nameof(entity));
            }
            
            _entities[entity.Id] = entity;
            entity.Manager = this;
            
            (entity as IRegistrationCallbacks)?.OnRegister();

            entity.OnComponentAdd
                .Subscribe(component =>
                {
                    if (entity.IsLocal())
                    {
                        BroadcastLocalComponentAdd(entity, component);
                    }
                });

            entity.OnComponentRemove
                .Subscribe(component =>
                {
                    if (entity.IsLocal())
                    {
                        BroadcastLocalComponentRemove(entity, component);
                    }
                });
        }

        /// <summary>
        /// Destroys all entities owned by the given player.
        /// </summary>
        /// This is done automatically if a player leaves and <see cref="DestroyPlayersAfterLeave"/> is true.
        /// 
        /// <param name="player">The player to destroy all entities of</param>
        /// 
        public void DestroyPlayerEntities(IPlayer player)
        {
            DestroyPlayerEntities(player.ClientId);
        }
        
        private void SendEntityAndPlayerUpdates()
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.IsLocal() && entity.IsActive)
                {
                    using (var stream = new MemoryStream())
                    {
                        _helper.SerializeInt(entity.Id, stream);
                        entity.Serialize(stream);

                        // entity.SerializeComponents resulted in an empty stream; skipping it
                        if (stream.Position > sizeof(int))
                        {
                            _connection.SendEvent(DefaultEvents.NetworkEntityUpdate, stream.ToArray(), entity.Reliable);
                        }
                    }
                }
            }

            if (_localPlayer != null)
            {
                if (PreparePlayerCache())
                {
                    using (var stream = new MemoryStream())
                    {
                        SerializePlayerWithCache(stream);
                        _connection.SendEvent(DefaultEvents.PlayerUpdate, stream.ToArray());
                    }
                }
            }
        }

        private IObservable<NetworkEntityManager> WaitForPlayerList()
        {
            RefreshPlayerClientId();
            
            if (_connection.IsServer())
            {
                SubscribeToEvents();
                return Observable.Return(this);
            }
            
            // Broadcast the player after joining a room
            BroadcastLocalPlayer();
            
            return _connection.OnEvent(DefaultEvents.PlayerList)
                .First()
                .Timeout(TimeSpan.FromMilliseconds(PlayerListTimeout))
                .Select(evt =>
                {
                    ReadPlayerList((byte[]) evt.Data);
                    
                    SubscribeToEvents();
                    return this;
                });
        }

        private void SubscribeToEvents()
        {
            _playerBroadcastSubscription = _connection.OnEvent(DefaultEvents.PlayerBroadcast)
                .Subscribe(evt =>
                {
                    var data = (byte[]) evt.Data;
                    using (var stream = new MemoryStream(data))
                    {
                        var player = ReadAndSavePlayer(stream);
                        _playerJoinedSubject.OnNext(player);
                    }

                    if (_localPlayer.IsServer())
                    {
                        // Broadcast the player list to the new player
                        SendPlayerList(evt.SenderId);
                    }
                });

            _connection.OnClientLeave.Subscribe(client =>
            {
                
                // Remove the player
                if (DestroyPlayersAfterLeave)
                {
                    if (_players.ContainsKey(client.ClientId))
                    {
                        _playerLeftSubject.OnNext(_players[client.ClientId]);
                        _players.Remove(client.ClientId);
                    }
                }

                // Destroy the player's entities
                if (DestroyPlayerEntitiesAfterLeave)
                {
                    DestroyPlayerEntities(client.ClientId);
                }
            });
        }

        private void DestroyPlayerEntities(int clientId)
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.OwnerId == clientId)
                {
                    UnregisterEntity(entity.Id, false);
                }
            }
        }

        private void RegisterLocalPlayer()
        {   
            _players[_localPlayer.ClientId] = _localPlayer;
            _localPlayer.Manager = this;
            
            (_localPlayer as IRegistrationCallbacks)?.OnRegister();

            BroadcastLocalPlayer();
        }
        
        private void BroadcastLocalPlayer()
        {
            PreparePlayerCache();
            using (var stream = new MemoryStream())
            {
                SerializePlayerWithCache(stream);
                _connection.SendEvent(DefaultEvents.PlayerBroadcast, stream.ToArray());
            }
        }
        
        private void BroadcastEntityCreation(INetworkEntity entity)
        {
            using (var stream = new MemoryStream())
            {
                _helper.SerializeInt(entity.Id, stream);
                _helper.SerializeInt(entity.OwnerId, stream);
                _connection.SendEvent(DefaultEvents.NetworkEntityCreate, stream.ToArray());
            }
        }

        private void RegisterRemoteEntity(byte[] entityData)
        {
            INetworkEntity entity;
            using (var stream = new MemoryStream(entityData))
            {
                entity = new NetworkEntity(_helper.DeserializeInt(stream), _helper.DeserializeInt(stream));
            }

            RegisterEntity(entity);
            _entityCreatedSubject.OnNext(entity);
        }

        private void UnregisterEntity(int id, bool checkLocalOwner)
        {
            if (!_entities.ContainsKey(id))
            {
                throw new ArgumentException("The given entity ID is not registered.", nameof(id));
            }
            
            var entity = _entities[id];

            if (checkLocalOwner && !entity.IsLocal())
            {
                throw new InvalidOperationException("Attempted to destroy an entity that is not owned by the local client.");
            }
            
            (entity as IRegistrationCallbacks)?.OnRemove();

            // Remove all components first
            entity.RemoveAllNetworkComponents();

            _entities.Remove(id);
            _entityDestroyedSubject.OnNext(entity);
        }

        private void BroadcastLocalComponentAdd(INetworkEntity entity, INetworkComponent component)
        {
            using (var stream = new MemoryStream())
            {
                _helper.SerializeInt(entity.Id, stream);
                _helper.SerializeShort(component.Id, stream);
                _helper.SerializeString(component.GetType().AssemblyQualifiedName, stream);
                component.Serialize(stream);
                _connection.SendEvent(DefaultEvents.NetworkComponentAdd, stream.ToArray());
            }
        }

        private void BroadcastLocalComponentRemove(INetworkEntity entity, INetworkComponent component)
        {
            using (var stream = new MemoryStream())
            {
                _helper.SerializeInt(entity.Id, stream);
                _helper.SerializeShort(component.Id, stream);
                _connection.SendEvent(DefaultEvents.NetworkComponentRemove, stream.ToArray());
            }
        }

        private void RegisterRemoteComponent(byte[] componentData)
        {
            var stream = new MemoryStream(componentData);

            int entityId = _helper.DeserializeInt(stream);
            short componentId = _helper.DeserializeShort(stream);
            string typeName = _helper.DeserializeString(stream);

            if (!_entities.ContainsKey(entityId))
            {
                throw new InvalidOperationException(
                    $"Cannot register component since no entity with ID {entityId} exists.");
            }

            var entity = _entities[entityId];

            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new InvalidOperationException("Type could not be loaded.");
            }

            var component = (INetworkComponent) Activator.CreateInstance(type);
            component.Id = componentId;
            component.Entity = entity;

            // Initialize with sent data
            component.Deserialize(stream);

            entity.AddNetworkComponent(component);
        }

        private void UnregisterRemoteComponent(byte[] componentData)
        {
            using (var stream = new MemoryStream(componentData))
            {
                int entityId = _helper.DeserializeInt(stream);
                short componentId = _helper.DeserializeShort(stream);

                if (!_entities.ContainsKey(entityId))
                {
                    throw new InvalidOperationException(
                        $"Cannot unregister component since no entity with ID {entityId} exists.");
                }

                var entity = _entities[entityId];
                entity.RemoveNetworkComponent(componentId);
            }
        }
        
        private void SendPlayerList(int clientId)
        {
            using (var stream = new MemoryStream())
            {
                var players = _players.Values
                    .Where(p => p.ClientId != clientId) // Skip the player that asked for the list
                    .ToArray();
                
                _helper.SerializeInt(players.Length, stream);
                foreach (var player in players)
                {
                    _helper.SerializeInt(player.ClientId, stream);
                    player.Serialize(stream);
                }
                
                _connection.SendEvent(DefaultEvents.PlayerList, stream.ToArray(), _connection.GetClient(clientId));
            }
        }

        private void ReadPlayerList(byte[] data)
        {
            _players.Clear();
            RefreshPlayerClientId();
            
            using (var stream = new MemoryStream(data))
            {
                int playerNum = _helper.DeserializeInt(stream);
                for (int i = 0; i < playerNum; i++)
                {
                    ReadAndSavePlayer(stream);
                }
            }
        }

        private IPlayer ReadAndSavePlayer(Stream stream)
        {
            var player = CreateTypedPlayer();

            player.ClientId = _helper.DeserializeInt(stream);
            player.Deserialize(stream);

            _players[player.ClientId] = player;
            return player;
        }

        private void UpdatePlayer(Stream stream)
        {
            int clientId = _helper.DeserializeInt(stream);
            if (_players.ContainsKey(clientId))
            {
                var player = _players[clientId];
                player.Deserialize(stream);
                _playerUpdatedSubject.OnNext(player);
            }
        }

        private bool PreparePlayerCache()
        {
            byte[] serializedPlayer;
            using (var tempStream = new MemoryStream())
            {
                _localPlayer.Serialize(tempStream);
                serializedPlayer = tempStream.ToArray();
            }

            if (_localPlayerCache == null || !UbernetUtils.AreArraysEqual(serializedPlayer, _localPlayerCache))
            {
                _localPlayerCache = serializedPlayer;
                return true;
            }

            return false;
        }

        private void SerializePlayerWithCache(Stream stream)
        {
            _helper.SerializeInt(_connection.LocalClient.ClientId, stream);
            stream.Write(_localPlayerCache, 0, _localPlayerCache.Length);
        }

        private IPlayer CreateTypedPlayer()
        {
            IPlayer player;
            try
            {
                player = (IPlayer) Activator.CreateInstance(_localPlayer.GetType());
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("A remote player instance could not be created. " +
                    "This error most likely originates from using different types for player objects " +
                    "on different clients (all clients in the same room must use player objects of the same " +
                    "type). Please check out the InnerException for more information.", e);
            }

            return player;
        }

        private void RegisterEvents()
        {
            _eventSubscription = _connection.OnEvent.Subscribe(evt =>
            {
                switch (evt.Code)
                {
                    case DefaultEvents.NetworkEntityCreate:
                        RegisterRemoteEntity((byte[]) evt.Data);
                        break;
                    case DefaultEvents.NetworkEntityDestroy:
                        UnregisterEntity((int) evt.Data, false);
                        break;
                    case DefaultEvents.NetworkComponentAdd:
                        RegisterRemoteComponent((byte[]) evt.Data);
                        break;
                    case DefaultEvents.NetworkComponentRemove:
                        UnregisterRemoteComponent((byte[]) evt.Data);
                        break;
                    case DefaultEvents.NetworkEntityUpdate:
                        var stream = new MemoryStream((byte[]) evt.Data);
                        int entityId = _helper.DeserializeInt(stream);
                        var entity = _entities[entityId];
                        if (entity.IsLocal())
                        {
                            throw new InvalidOperationException(
                                $"Remote client tried to update local entity: {entity}");
                        }

                        entity.Deserialize(stream);
                        _entityUpdatedSubject.OnNext(entity);
                        break;
                    case DefaultEvents.PlayerUpdate:
                        using (stream = new MemoryStream((byte[]) evt.Data))
                        {
                            UpdatePlayer(stream);
                        }
                        break;
                }
            });
        }
        
        private void RefreshPlayerClientId()
        {
            int oldClientId = _localPlayer.ClientId;
            int newClientId = _connection.LocalClient.ClientId;

            if (newClientId != oldClientId)
            {
                _players.Remove(oldClientId);
                _players[newClientId] = _localPlayer;
                _localPlayer.ClientId = _connection.LocalClient.ClientId;
            }

            if (!_players.ContainsValue(_localPlayer))
            {
                _players[newClientId] = _localPlayer;
            }
        }
    }
}