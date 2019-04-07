using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Skaillz.Ubernet.NetworkEntities.Unity;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Skaillz.Ubernet.NetworkEntities
{
    public class NetworkEntityManager
    {
        /// <summary>
        /// The times per second Entity and Player updates are sent by default
        /// </summary>
        public const int DefaultSerializationRate = 20;
        public const int MinSafeEntityId = 1000000;
        public const int MaxEntitiesPerPlayer = MinSafeEntityId / 1000;
        private const double PlayerListTimeout = 10000.0;

        private readonly Dictionary<int, INetworkEntity> _entities = new Dictionary<int, INetworkEntity>();
        private readonly Dictionary<int, IPlayer> _players = new Dictionary<int, IPlayer>();
        private IConnection _connection;
        private IPlayer _localPlayer;
        private byte[] _localPlayerCache;

        private int _nextEntityId;
        private IDisposable _eventSubscription;
        private IDisposable _playerBroadcastSubscription;

        private readonly ISubject<INetworkEntity> _entityCreatedSubject = new Subject<INetworkEntity>();
        private readonly ISubject<INetworkEntity> _entityDestroyedSubject = new Subject<INetworkEntity>();
        private readonly ISubject<INetworkEntity> _entityUpdatedSubject = new Subject<INetworkEntity>();
        
        private readonly ISubject<IPlayer> _playerJoinedSubject = new Subject<IPlayer>();
        private readonly ISubject<IPlayer> _playerLeftSubject = new Subject<IPlayer>();
        private readonly ISubject<IPlayer> _playerUpdatedSubject = new Subject<IPlayer>();

        private readonly MemoryStream _stream = new MemoryStream(8192);

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
        /// <param name="ownedByScene">
        /// If false, the newly created entity will be owned by the local player. If true, it will be owned by the scene,
        /// which means that it's always controlled by the server.
        /// </param>
        /// <exception cref="ArgumentNullException">If the entity is null</exception>
        /// <exception cref="OverflowException">If the entity manager has run out of usable IDs since too many entities
        /// have been created</exception>
        public virtual void InstantiateEntity([NotNull] INetworkEntity entity, bool ownedByScene = false)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity must not be null.");
            }

            entity.Id = CreateEntityId();
            
            if (_entities.ContainsKey(entity.Id))
            {
                throw new InvalidOperationException($"An entity with ID {entity.Id} has already been registered.");
            }

            entity.OwnerId = ownedByScene ? -1 : _connection.LocalClient.ClientId;

            BroadcastEntityCreation(entity);
            RegisterEntity(entity, true);
        }
        
         public GameObjectNetworkEntityBase InstantiateFromResourcePrefab(string path, Vector3 position = default(Vector3),
             Quaternion rotation = default(Quaternion))
        {
            GameObjectNetworkEntity.AutoRegister = false;
            
            try
            {
                var prefab = Resources.Load(path) as GameObject;
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Prefab could not be loaded from path: '{path}'");
                }
                
                if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
                {
                    throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                        $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
                }

                var go = Object.Instantiate(prefab, position, rotation);
                var entity = go.GetComponent<GameObjectNetworkEntityBase>();

                entity.Id = CreateEntityId();
                entity.OwnerId = LocalPlayer.ClientId;

                _stream.Clear();
                
                SerializationHelper.SerializeInt(entity.Id, _stream);
                SerializationHelper.SerializeInt(entity.OwnerId, _stream);
                SerializationHelper.SerializeString(path, _stream);
                
                UnityUtils.Vector3Serializer.Serialize(position, _stream);
                UnityUtils.QuaternionSerializer.Serialize(rotation, _stream);
                
                Connection.SendEvent(DefaultEvents.NetworkEntityCreateFromResource, _stream.ToArray());

                RegisterEntity(entity, true);
                return entity;
            }
            finally
            {
                GameObjectNetworkEntity.AutoRegister = true;
            }
        }
        
        public GameObjectNetworkEntityBase InstantiateFromPrefab(GameObject prefab, Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion))
        {
            GameObjectNetworkEntity.AutoRegister = false;
            
            try
            {
                var prefabCache = PrefabCache.GetPrefabCache();
                int cacheIndex = prefabCache.GetPrefabIndex(prefab);
                if (cacheIndex == -1)
                {
                    throw new InvalidOperationException("The given prefab is not in the prefab cache. Please add it and try again.");
                }
                
                if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
                {
                    throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                        $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
                }

                var go = Object.Instantiate(prefab, position, rotation);
                var entity = go.GetComponent<GameObjectNetworkEntityBase>();

                entity.Id = CreateEntityId();
                entity.OwnerId = LocalPlayer.ClientId;

                _stream.Clear();
                SerializationHelper.SerializeInt(entity.Id, _stream);
                SerializationHelper.SerializeInt(entity.OwnerId, _stream);
                SerializationHelper.SerializeInt(cacheIndex, _stream);
                
                UnityUtils.Vector3Serializer.Serialize(position, _stream);
                UnityUtils.QuaternionSerializer.Serialize(rotation, _stream);
                
                Connection.SendEvent(DefaultEvents.NetworkEntityCreateFromPrefabCache, _stream.ToArray());

                RegisterEntity(entity, true);
                return entity;
            }
            finally
            {
                GameObjectNetworkEntity.AutoRegister = true;
            }
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
        /// Destroys given entity on all clients.
        /// </summary>
        /// Unregisters the given entity and sends its destroy event to all other clients.
        /// <param name="entity">The entity to destroy</param>
        public void DestroyEntity(INetworkEntity entity)
        {
            DestroyEntity(entity.Id);
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
            RegisterEntity(entity, false);
        }

        internal void RegisterEntity([NotNull] INetworkEntity entity, bool triggerEvent)
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
            
            (entity as IRegistrationCallbacks)?.OnRegister();

            if (triggerEvent)
            {
                _entityCreatedSubject.OnNext(entity);
            }
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
        
        /// <summary>
        /// Returns an unused entity ID that can be assigned to new entities
        /// </summary>
        /// <exception cref="OverflowException">If the maximum number of entities has been exceeded</exception>
        public virtual int CreateEntityId(bool ownedByScene = false)
        {
            // TODO: this doesn't support scene objects well enough (only one next entity ID is saved)
            int ownerId = ownedByScene ? -1 : _localPlayer.ClientId;
            int firstEntityId = (ownerId + 1) * MaxEntitiesPerPlayer;
            
            if (_nextEntityId + 1 >= MinSafeEntityId 
                || _nextEntityId + 1 >= firstEntityId + MaxEntitiesPerPlayer 
                || _nextEntityId <= 0
                || _nextEntityId < firstEntityId)
            {
                _nextEntityId = firstEntityId;
            }

            // Try to find an unused safe ID
            bool foundSafeId = false;
            for (; _nextEntityId < firstEntityId + MaxEntitiesPerPlayer && _nextEntityId < MinSafeEntityId; _nextEntityId++)
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

            
            return _nextEntityId;
        }

        /// <summary>
        /// Returns if the given entity is registered at this manager
        /// </summary>
        /// <param name="entity">The entity to get the registration status for</param>
        /// <returns>True if the entity is registered</returns>
        public bool IsEntityRegistered(INetworkEntity entity)
        {
            return _entities.ContainsKey(entity.Id);
        }

        /// <summary>
        /// Returns the entity with the given ID.
        /// </summary>
        /// <param name="id">The ID to return the entity for</param>
        /// <returns>The entity with the given ID</returns>
        public INetworkEntity GetEntity(int id)
        {
            if (!_entities.ContainsKey(id))
            {
                return null;
            }

            return _entities[id];
        }
        
        /// <summary>
        /// Removes all entities. Useful when loading a new scene after entities were registered for another scene.
        /// </summary>
        public void UnregisterAllEntities()
        {
            _entities.Clear();
        }
        
        private void SendEntityAndPlayerUpdates()
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.IsLocal() && entity.IsActive)
                {
                    _stream.Clear();
                    SerializationHelper.SerializeInt(entity.Id, _stream);
                    entity.Serialize(_stream);

                    // entity.SerializeComponents resulted in an empty _stream; skipping it
                    if (_stream.Length > sizeof(int))
                    {
                        _connection.SendEvent(DefaultEvents.NetworkEntityUpdate, _stream.ToArray(), entity.Reliable);
                    }
                }
            }

            if (_localPlayer != null)
            {
                if (PreparePlayerCache())
                {
                    _stream.Clear();
                
                    SerializePlayerWithCache();
                    _connection.SendEvent(DefaultEvents.PlayerUpdate, _stream.ToArray());
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
                    
                    _stream.From(data);
                    var player = ReadAndSavePlayer();
                    _playerJoinedSubject.OnNext(player);

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
            
            _stream.Clear();
            SerializePlayerWithCache();
            _connection.SendEvent(DefaultEvents.PlayerBroadcast, _stream.ToArray());
        }
        
        private void BroadcastEntityCreation(INetworkEntity entity)
        {
            _stream.Clear();
            
            SerializationHelper.SerializeInt(entity.Id, _stream);
            SerializationHelper.SerializeInt(entity.OwnerId, _stream);
            SerializationHelper.SerializeString(entity.GetType().AssemblyQualifiedName, _stream);
            _connection.SendEvent(DefaultEvents.NetworkEntityCreate, _stream.ToArray());
        }

        private void RegisterRemoteEntity(byte[] entityData)
        {
            _stream.From(entityData);
            
            int id = SerializationHelper.DeserializeInt(_stream);
            int ownerId = SerializationHelper.DeserializeInt(_stream);
            string typeName = SerializationHelper.DeserializeString(_stream);

            var type = Type.GetType(typeName);
            
            INetworkEntity entity;
            if (type.IsSubclassOf(typeof(GameObjectNetworkEntityBase)))
            {
                var go = new GameObject($"Remote NetworkEntity #{id}");
                entity = (INetworkEntity) go.AddComponent(type);
            }
            else
            {
                entity = (INetworkEntity) Activator.CreateInstance(type);
            }

            entity.Id = id;
            entity.OwnerId = ownerId;
            
            RegisterEntity(entity);
            _entityCreatedSubject.OnNext(entity);
        }
        
        private void RegisterRemoteEntityFromResource(byte[] entityData)
        {
            INetworkEntity entity;
            
            _stream.From(entityData);
            
            int id = SerializationHelper.DeserializeInt(_stream);
            int ownerId = SerializationHelper.DeserializeInt(_stream);
            string path = SerializationHelper.DeserializeString(_stream);
            
            var position = UnityUtils.Vector3Serializer.Deserialize(_stream);
            var rotation = UnityUtils.QuaternionSerializer.Deserialize(_stream);

            var prefab = Resources.Load(path) as GameObject;
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab from remote instantiation could not be loaded from path: '{path}'");
            }
            
            if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
            {
                throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                    $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
            }

            GameObjectNetworkEntity.AutoRegister = false;
            var go = Object.Instantiate(prefab, position, rotation);
            entity = go.GetComponent<GameObjectNetworkEntityBase>();

            entity.Id = id;
            entity.OwnerId = ownerId;
            
            RegisterEntity(entity);
            GameObjectNetworkEntity.AutoRegister = true;
            _entityCreatedSubject.OnNext(entity);
        }
        
        private void RegisterRemoteEntityFromPrefabCache(byte[] entityData)
        {
            _stream.From(entityData);
            
            int id = SerializationHelper.DeserializeInt(_stream);
            int ownerId = SerializationHelper.DeserializeInt(_stream);
            int cacheIndex = SerializationHelper.DeserializeInt(_stream);

            var position = UnityUtils.Vector3Serializer.Deserialize(_stream);
            var rotation = UnityUtils.QuaternionSerializer.Deserialize(_stream);

            var prefab = PrefabCache.GetPrefabCache().GetPrefab(cacheIndex);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab from remote instantiation could not be loaded from cache.'");
            }
            
            if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
            {
                throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                    $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
            }

            GameObjectNetworkEntity.AutoRegister = false;
            var go = Object.Instantiate(prefab, position, rotation);
            
            INetworkEntity entity = go.GetComponent<GameObjectNetworkEntityBase>();

            entity.Id = id;
            entity.OwnerId = ownerId;
            
            RegisterEntity(entity);
            GameObjectNetworkEntity.AutoRegister = true;
            _entityCreatedSubject.OnNext(entity);
            Debug.Log("Registered remote entity: " + entity);
        }

        internal void UnregisterEntity(int id, bool checkLocalOwner)
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

            entity.Manager = null;

            // Remove all components first
            entity.RemoveAllNetworkComponents();

            _entities.Remove(id);
            _entityDestroyedSubject.OnNext(entity);
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

        private void BroadcastLocalComponentAdd(INetworkEntity entity, INetworkComponent component)
        {
            _stream.Clear();
            
            SerializationHelper.SerializeInt(entity.Id, _stream);
            SerializationHelper.SerializeShort(component.Id, _stream);
            SerializationHelper.SerializeString(component.GetType().AssemblyQualifiedName, _stream);
            component.Serialize(_stream);
            _connection.SendEvent(DefaultEvents.NetworkComponentAdd, _stream.ToArray());
        }

        private void BroadcastLocalComponentRemove(INetworkEntity entity, INetworkComponent component)
        {
            _stream.Clear();
            
            SerializationHelper.SerializeInt(entity.Id, _stream);
            SerializationHelper.SerializeShort(component.Id, _stream);
            _connection.SendEvent(DefaultEvents.NetworkComponentRemove, _stream.ToArray());
        }

        private void RegisterRemoteComponent(byte[] componentData)
        {
            _stream.From(componentData);
            
            int entityId = SerializationHelper.DeserializeInt(_stream);
            short componentId = SerializationHelper.DeserializeShort(_stream);
            string typeName = SerializationHelper.DeserializeString(_stream);

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

            INetworkComponent component;
            if (type.IsSubclassOf(typeof(MonoNetworkComponent)))
            {
                if (!(entity is GameObjectNetworkEntityBase))
                {
                    throw new UbernetException(
                        $"Adding {nameof(MonoNetworkComponent)}s is only supported on entities of " +
                        $"type {nameof(GameObjectNetworkEntityBase)}.");
                }

                var goEntity = (GameObjectNetworkEntityBase) entity;
                component = (INetworkComponent) goEntity.gameObject.AddComponent(type);
            }
            else
            {
                component = (INetworkComponent) Activator.CreateInstance(type);
            }

            component.Id = componentId;
            component.Entity = entity;

            (component as IRegistrationCallbacks)?.OnRegister();

            // Initialize with sent data
            component.Deserialize(_stream);

            entity.AddNetworkComponent(component);
        }

        private void UnregisterRemoteComponent(byte[] componentData)
        {
            _stream.From(componentData);
            
            int entityId = SerializationHelper.DeserializeInt(_stream);
            short componentId = SerializationHelper.DeserializeShort(_stream);

            if (!_entities.ContainsKey(entityId))
            {
                throw new InvalidOperationException(
                    $"Cannot unregister component since no entity with ID {entityId} exists.");
            }

            var entity = _entities[entityId];
            entity.RemoveNetworkComponent(componentId);
        }
        
        private void SendPlayerList(int clientId)
        {
            _stream.Clear();
            
            var players = _players.Values
                .Where(p => p.ClientId != clientId) // Skip the player that asked for the list
                .ToArray();
            
            SerializationHelper.SerializeInt(players.Length, _stream);
            foreach (var player in players)
            {
                SerializationHelper.SerializeInt(player.ClientId, _stream);
                player.Serialize(_stream);
            }
            
            _connection.SendEvent(DefaultEvents.PlayerList, _stream.ToArray(), _connection.GetClient(clientId));
        }

        private void ReadPlayerList(byte[] data)
        {
            _players.Clear();
            RefreshPlayerClientId();
            
            _stream.From(data);
            
            int playerNum = SerializationHelper.DeserializeInt(_stream);
            for (int i = 0; i < playerNum; i++)
            {
                ReadAndSavePlayer();
            }
        }

        private IPlayer ReadAndSavePlayer()
        {
            var player = CreateTypedPlayer();

            player.ClientId = SerializationHelper.DeserializeInt(_stream);

            _players[player.ClientId] = player;

            player.Manager = this;
            (player as IRegistrationCallbacks)?.OnRegister();
            player.Deserialize(_stream);
            return player;
        }

        private void UpdatePlayer()
        {
            int clientId = SerializationHelper.DeserializeInt(_stream);
            if (_players.ContainsKey(clientId))
            {
                var player = _players[clientId];
                player.Deserialize(_stream);
                _playerUpdatedSubject.OnNext(player);
            }
        }

        private bool PreparePlayerCache()
        {
            _stream.Clear();
            _localPlayer.Serialize(_stream);
            
            var serializedPlayer = _stream.ToArray();

            if (_localPlayerCache == null || !UbernetUtils.AreArraysEqual(serializedPlayer, _localPlayerCache))
            {
                _localPlayerCache = serializedPlayer;
                return true;
            }

            return false;
        }

        private void SerializePlayerWithCache()
        {
            _stream.Clear();
            
            SerializationHelper.SerializeInt(_connection.LocalClient.ClientId, _stream);
            _stream.Write(_localPlayerCache, 0, _localPlayerCache.Length);
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
                    case DefaultEvents.NetworkEntityCreateFromResource:
                        RegisterRemoteEntityFromResource((byte[]) evt.Data);
                        break;
                    case DefaultEvents.NetworkEntityCreateFromPrefabCache:
                        RegisterRemoteEntityFromPrefabCache((byte[]) evt.Data);
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
                        var data = (byte[]) evt.Data;
                        _stream.From(data);

                        int entityId = SerializationHelper.DeserializeInt(_stream);
                        if (_entities.ContainsKey(entityId))
                        {
                            var entity = _entities[entityId];
                            if (entity.IsLocal())
                            {
                                throw new InvalidOperationException(
                                    $"Remote client tried to update local entity: {entity}");
                            }

                            entity.Deserialize(_stream);
                            _entityUpdatedSubject.OnNext(entity);
                        }
                        else
                        {
                            Debug.LogWarning($"Tried to update unknown entity with ID {entityId}");
                        }

                        break;
                    case DefaultEvents.PlayerUpdate:
                        data = (byte[]) evt.Data;

                        _stream.From(data);
                        UpdatePlayer();
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