using System;

namespace Skaillz.Ubernet
{
    /// <summary>
    /// Interface for connections that can disconnect asynchronously.
    /// </summary>
    /// <typeparam name="T">The type the <see cref="Disconnect"/> method's observable resolves to</typeparam>
    public interface IDisconnectable<T>
    {
        /// <summary>
        /// Disconnects the resource, returning an <see cref="IObservable{T}"/> that resolves then finished
        /// </summary>
        /// <returns>An observable that resolves when the connection has disconnected</returns>
        IObservable<T> Disconnect();
    }
}