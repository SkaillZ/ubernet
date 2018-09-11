using System;
using System.Linq;
using UniRx;

namespace Skaillz.Ubernet
{
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Sends an event with the given code and data to all other clients.
        /// </summary>
        /// <param name="connection">The connection to send the event from</param>
        /// <param name="code">The event code that identifies the event. Avoid sending a code that is present in <see cref="DefaultEvents"/>.</param>
        /// <param name="data">The event data. Its type must be serializable be the connection's <see cref="IConnection.Serializer"/>.</param>
        /// <param name="reliable">Whether the event should be sent reliably (if supported by the connection).</param>
        public static void SendEvent(this IConnection connection, byte code, object data, bool reliable = true)
        {
            connection.SendEvent(code, data, MessageTarget.Others, reliable);
        }
        
        /// <summary>
        /// Returns an observable that can be used to describe to network events of the given code
        /// </summary>
        /// <param name="connection">The connection to receive events from</param>
        /// <param name="code">The event code that identifies the event</param>
        /// <returns>An observable with network events</returns>
        public static IObservable<NetworkEvent> OnEvent(this IConnection connection, byte code)
        {
            return connection.OnEvent.Where(evt => evt.Code == code);
        }
        
        public static bool IsServer(this IConnection connection)
        {
            return connection.LocalClient.ClientId != -1 && connection.LocalClient.ClientId == connection.Server.ClientId;
        }
        
        public static bool IsServer(this IConnection connection, IClient client)
        {
            return client.ClientId == connection.Server.ClientId;
        }
    }
}