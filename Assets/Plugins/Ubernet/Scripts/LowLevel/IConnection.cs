using System;
using System.Collections.Generic;

namespace Skaillz.Ubernet
{
    /// <summary>
    /// A generic network connection that can send and receive events.
    /// </summary>
    public interface IConnection : IDisconnectable<IConnection>, IUpdateable
    {
        /// <summary>
        /// The connection's serializer.
        /// </summary>
        /// Whenever an event is sent, the serializer converts it into bytes. Custom types must be registered
        /// on the serializer before they can be sent with <see cref="SendEvent"/>.
        ISerializer Serializer { get; }
        
        /// <summary>
        /// The local client of the connection.
        /// </summary>
        IClient LocalClient { get; }
        
        /// <summary>
        /// The server's (or master client's) <see cref="IClient"/> representation.
        /// </summary>
        IClient Server { get; }
        
        /// <summary>
        /// A list of clients connected to the same game.
        /// </summary>
        IReadOnlyList<IClient> Clients { get; }
        
        /// <summary>
        /// Returns if the connection is still available.
        /// </summary>
        /// Connections should always be connected initially, but can become disconnected by timeouts, etc.
        /// Subscribe to <see cref="OnDisconnected"/> to handle disconnects in your game logic.
        bool IsConnected { get; }
        
        /// <summary>
        /// Returns the server's time in seconds.
        /// </summary>
        ///
        /// This value should be set once and remain constant. Which time is used depends on the implementation.
        double ServerTime { get; }
        
        /// <summary>
        /// The current round trip time from this client to the server in milliseconds.
        /// </summary>
        /// This value is updated whenever <see cref="PingServer"/> is called.
        long CurrentRoundTripTime { get; }
        
        /// <summary>
        /// The interval between auto-pings in seconds. Set it to zero to disable auto pinging.
        /// </summary>
        float AutoPingInterval { get; }
        
        /// <summary>
        /// Returns whether the connection supports host migration.
        /// </summary>
        /// If true, <see cref="OnHostMigration"/> and <see cref="MigrateHost"/> can be used.
        bool SupportsHostMigration { get; }
        
        /// <summary>
        /// Determines if any events should be sent over the network. Events can be paused by setting this property to false
        /// (useful while switching scenes)
        /// </summary>
        bool SendEvents { get; set; }
        
        /// <summary>
        /// An observable which subscriptions are called when the connection is dropped, e.g. by a timeout, calling
        /// <see cref="IConnection.Disconnect()"/>, etc.
        /// </summary>
        IObservable<DisconnectReason> OnDisconnected { get; }
        
        /// <summary>
        /// Called when a remote client connects.
        /// </summary>
        IObservable<IClient> OnClientJoin { get; }
        
        /// <summary>
        /// Called when a remote client disconnects from this connection.
        /// </summary>
        IObservable<IClient> OnClientLeave { get; }
        
        /// <summary>
        /// Called when a the host was migrated, either by a player leaving the room or <see cref="MigrateHost"/> being called.
        /// </summary>
        /// <exception cref="InvalidOperationException">If host migration is not supported</exception>
        IObservable<IClient> OnHostMigration { get; }
        
        /// <summary>
        /// An observable that can be used to describe to network events
        /// </summary>
        IObservable<NetworkEvent> OnEvent { get; }

        /// <summary>
        /// Sets the given client as the new host.
        /// </summary>
        /// Note that this only works if the current client is the master client. It will also take a while until
        /// the master client is updated on all other clients.
        /// 
        /// <param name="newHostId">The client ID of the new host</param>
        /// <exception cref="InvalidOperationException">If called by a non-master client or host migration is not supported</exception>
        void MigrateHost(int newHostId);

        /// <summary>
        /// Returns a client by the given ID
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <returns>The <see cref="IClient"/> object of the client with the given ID</returns>
        IClient GetClient(int clientId);

        /// <summary>
        /// Sends an event with the given code and data to the given target clients.
        /// </summary>
        /// <param name="code">The event code that identifies the event. Avoid sending a code that is present in <see cref="DefaultEvents"/>.</param>
        /// <param name="data">The event data. Its type must be serializable be the connection's <see cref="IConnection.Serializer"/>.</param>
        /// <param name="target">
        /// The targets who should receive the event.
        /// Use <see cref="MessageTarget"/>.<see cref="MessageTarget.AllPlayers"/> to send the message to all clients,
        /// <see cref="MessageTarget"/>.<see cref="MessageTarget.Others"/> to send it to other clients (default),
        /// <see cref="MessageTarget"/>.<see cref="MessageTarget.Server"/> to send it to the server.
        /// You can also pass a <see cref="IClient"/>, <see cref="ClientList"/> or any other <see cref="IClientIdResolvable"/>
        /// or <see cref="IClientIdListResolvable"/>.
        /// </param>
        /// <param name="reliable">Whether the event should be sent reliably (if supported by the connection).</param>
        void SendEvent(byte code, object data, IMessageTarget target, bool reliable = true);

        /// <summary>
        /// Returns the round trip time to the <see cref="Server"/> in milliseconds.
        /// </summary>
        /// <returns>An Observable that returns the round trip time in milliseconds</returns>
        IObservable<long> PingServer();

        /// <summary>
        /// Returns the round trip time to the given Client in milliseconds.
        /// </summary>
        /// <param name="client">The client to ping</param>
        /// <returns>An Observable that returns the round trip time in milliseconds</returns>
        IObservable<long> Ping(IClient client);
    }
}