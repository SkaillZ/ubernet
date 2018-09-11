using System;

namespace Skaillz.Ubernet.Providers.Photon
{
    public static class PhotonExtensions
    {
        /// <summary>
        /// Connects to a given <see cref="IGame"/> with <see cref="PhotonConnectionProvider"/>
        /// </summary>
        /// <exception cref="UnsupportedGameTypeException">
        /// Thrown if the game's type and the provider are not compatible
        /// </exception>
        /// <param name="game">The game to connect to</param>
        /// <returns>An <see cref="IObservable{T}"/> that resolves with a game or calls <see cref="IObserver{T}.OnError"/>
        /// with an exception if the connection failed.
        /// </returns>
        public static IObservable<IConnection> ConnectWithPhoton(this IGame game)
        {
            return new PhotonConnectionProvider().Connect(game);
        }
    }
}