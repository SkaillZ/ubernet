using System;

namespace Skaillz.Ubernet
{
    public interface IMatchmaker
    {
        /// <summary>
        /// Creates a game with the given options
        /// </summary>
        /// <param name="options">Platform dependent options to create the game with</param>
        /// <returns>
        /// An <see cref="IObservable{T}"/> that resolves with the game if it has been created or or calls <see cref="IObserver{T}.OnError"/>
        /// with an exception if the game could not be created.
        /// </returns>
        IObservable<IGame> CreateGame(ICreateGameOptions options);
        
        /// <summary>
        /// Finds a game that matches the given query
        /// </summary>
        /// <param name="query">Platform dependent game query</param>
        /// <returns>
        /// An <see cref="IObservable{T}"/> that resolves with a found game or calls <see cref="IObserver{T}.OnError"/>
        /// with an exception if no game could be found.
        /// </returns>
        IObservable<IGame> FindGame(IGameQuery query);
        
        /// <summary>
        /// Finds a random game with the given options
        /// </summary>
        /// <param name="options">Platform dependent options for finding a random game</param>
        /// <returns>
        /// An <see cref="IObservable{T}"/> that resolves with a found game or or calls <see cref="IObserver{T}.OnError"/>
        /// with an exception if no game could be found.
        /// </returns>
        IObservable<IGame> FindRandomGame(IJoinRandomGameOptions options);
    }
}