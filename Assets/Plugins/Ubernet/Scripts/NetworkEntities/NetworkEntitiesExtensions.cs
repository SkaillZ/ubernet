using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Skaillz.Ubernet.NetworkEntities
{
    public static class NetworkEntitiesExtensions
    {
        /// <summary>
        /// Returns true if the entity belongs to the local client or false if it belongs to a remote client.
        /// </summary>
        /// <param name="entity">The entity to test for</param>
        /// <returns>true if the entity belongs to the local client or false if it belongs to a remote client</returns>
        /// <exception cref="InvalidOperationException">If the Entity is not initialized</exception>
        public static bool IsLocal(this INetworkEntity entity)
        {
            if (entity?.Manager == null)
            {
                return false;
            }
            
            var connection = entity.Manager?.Connection;
            int localClientId = connection.LocalClient.ClientId;

            if (localClientId == -1)
            {
                return false;
            }

            if (connection.IsServer() && entity.IsOwnedByScene())
            {
                return true;
            }
            
            return localClientId != -1 && entity.OwnerId == localClientId;
        }

        /// <summary>
        /// Returns the <see cref="IClient"/> the player belongs to
        /// </summary>
        /// <param name="player">The player to get the client from</param>
        /// <returns>The client the player belongs to</returns>
        public static IClient GetClient([NotNull] this IPlayer player)
        {
            return player.Manager?.Connection?.GetClient(player.ClientId);
        }
        
        /// <summary>
        /// Returns if the entity is owned by the scene.
        /// </summary>
        /// Scene-owned entities are controlled by the server.
        /// 
        /// <param name="entity">The entity to test for</param>
        /// <returns>Whether the entity is owned by the scene</returns>
        public static bool IsOwnedByScene([NotNull] this INetworkEntity entity)
        {
            return entity.OwnerId == -1;
        }
        
        /// <inheritdoc cref="IConnection.SendEvent"/>
        public static void SendEvent([NotNull] this NetworkEntityManager manager, byte code, object data, 
            IMessageTarget target, bool reliable = true)
        {
            manager.Connection?.SendEvent(code, data, target, reliable);
        }
        
        /// <inheritdoc cref="ConnectionExtensions.SendEvent"/>
        public static void SendEvent([NotNull] this NetworkEntityManager manager, byte code, object data, bool reliable = true)
        {
            manager.Connection?.SendEvent(code, data, reliable);
        }
        
        /// <summary>
        /// Returns an observable that can be used to describe to network events of the given code
        /// </summary>
        /// <param name="manager">The manager with the connection to receive events from</param>
        /// <param name="code">The event code that identifies the event</param>
        /// <returns>An observable with network events</returns>
        public static IObservable<NetworkEvent> OnEvent([NotNull] this NetworkEntityManager manager, byte code)
        {
            return manager.Connection?.OnEvent(code);
        }
        
        /// <summary>
        /// Returns the serializer of the entity manager's <see cref="IConnection"/>.
        /// </summary>
        /// See also: <seealso cref="IConnection.Serializer"/>
        /// 
        /// <param name="manager">The entity manager to get the serializer from</param>
        /// <returns>The serializer of the entity manager's <see cref="IConnection"/>.</returns>
        public static ISerializer GetSerializer([NotNull] this NetworkEntityManager manager)
        {
            return manager.Connection?.Serializer;
        }
        
        public static ISerializer GetSerializer([NotNull] this INetworkEntity entity)
        {
            return entity.Manager?.GetSerializer();
        }
        
        /// <summary>
        /// Returns the owning player.
        /// </summary>
        /// <param name="entity">The entity to receive the owning player from</param>
        /// <returns>The player that owns the entity.</returns>
        public static IPlayer GetOwner([NotNull] this INetworkEntity entity)
        {
            return entity.Manager?.GetPlayer(entity.OwnerId);
        }
        
        /// <summary>
        /// Returns the owning player.
        /// </summary>
        /// <param name="entity">The entity to receive the owning player from</param>
        /// <returns>The player that owns the entity.</returns>
        public static TPlayerEntity GetOwner<TPlayerEntity>([NotNull] this INetworkEntity entity) where TPlayerEntity : class, IPlayer
        {
            return entity.Manager?.GetPlayer(entity.OwnerId) as TPlayerEntity;
        }
        
        public static TPlayerEntity GetLocalPlayer<TPlayerEntity>([NotNull] this NetworkEntityManager manager) where TPlayerEntity : class, IPlayer
        {
            return manager.LocalPlayer as TPlayerEntity;
        }
        
        public static IPlayer GetServerPlayer([NotNull] this NetworkEntityManager manager)
        {
            return manager.GetPlayer(manager.Connection.Server.ClientId);
        }
        
        public static TPlayer GetPlayer<TPlayer>([NotNull] this NetworkEntityManager manager, int clientId) where TPlayer : class, IPlayer
        {
            return manager.GetPlayer(clientId) as TPlayer;
        }
        
        public static TPlayerEntity GetServerPlayer<TPlayerEntity>([NotNull] this NetworkEntityManager manager) where TPlayerEntity : class, IPlayer
        {
            return GetServerPlayer(manager) as TPlayerEntity;
        }
        
        public static IEnumerable<TPlayerEntity> GetPlayers<TPlayerEntity>([NotNull] this NetworkEntityManager manager) where TPlayerEntity : class, IPlayer
        {
            return manager.Players.Cast<TPlayerEntity>();
        }
        
        public static bool IsLocalPlayer([NotNull] this IPlayer player)
        {
            return player.ClientId == player.Manager?.LocalPlayer?.ClientId;
        }
        
        public static bool IsServer([NotNull] this IPlayer player)
        {
            return player.ClientId == player.Manager?.Connection?.Server?.ClientId;
        }
        
        /// <summary>
        /// Returns all the entities owned by the given player
        /// </summary>
        /// <param name="player">The player for which all all entities should be received</param>
        /// <returns>A list of entities owned by the given player</returns>
        public static IEnumerable<INetworkEntity> GetEntities([NotNull] this IPlayer player)
        {
            return player.Manager?.Entities.Where(entity => entity.OwnerId == player.ClientId);
        }
        
        public static T GetNetworkComponent<T>(this INetworkEntity entity) where T : INetworkComponent
        {
            return entity.Components.OfType<T>().FirstOrDefault();
        }
        
        public static T[] GetNetworkComponents<T>(this INetworkEntity entity) where T : INetworkComponent
        {
            return entity.Components.OfType<T>().ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="NetworkEntityManager"/> for the given connection.
        /// </summary>
        /// <param name="connection">The connection to create an entity manager for</param>
        /// <param name="serializationRate">The rate at which Entity and player Updates are sent per second</param>
        /// <returns></returns>
        public static NetworkEntityManager CreateEntityManager([NotNull] this IConnection connection, 
            int serializationRate = NetworkEntityManager.DefaultSerializationRate)
        {
            return NetworkEntityManager.Create(connection, serializationRate);
        }

        /// <summary>
        /// Destroys given entity on all clients.
        /// </summary>
        /// Unregisters the given entity and sends its destroy event to all other clients.
        /// <param name="entity">The entity to destroy</param>
        public static void Destroy(this INetworkEntity entity)
        {
            entity.Manager?.DestroyEntity(entity);
        }
    }
}