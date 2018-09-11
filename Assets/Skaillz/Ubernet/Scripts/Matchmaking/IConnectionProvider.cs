using System;

namespace Skaillz.Ubernet
{
    /// <summary>
    /// A provider that creates a connection for a given <see cref="IGame"/>.
    /// </summary>
    public interface IConnectionProvider
    {
        /// <summary>
        /// Connects to a given <see cref="IGame"/>
        /// </summary>
        /// <exception cref="UnsupportedGameTypeException">
        /// Thrown if the game's type and the provider are not compatible
        /// </exception>
        /// <param name="game">The game to connect to</param>
        /// <returns>An <see cref="IObservable{T}"/> that resolves with a game or calls <see cref="IObserver{T}.OnError"/>
        /// with an exception if the connection failed.
        /// </returns>
        IObservable<IConnection> Connect(IGame game);
    }
}