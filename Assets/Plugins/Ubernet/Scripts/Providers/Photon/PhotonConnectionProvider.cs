using System;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    
    /// <summary>
    /// Provider that creates a <see cref="PhotonRoomConnection"/> for a given <see cref="IGame"/>.
    /// </summary>
    public class PhotonConnectionProvider : IConnectionProvider
    {
        public IObservable<IConnection> Connect(IGame game)
        {
            if (game is PhotonAlreadyJoinedGame)
            {
                return Observable.Return(((PhotonAlreadyJoinedGame) game).GetConnection());
            }
            
            throw new UnsupportedGameTypeException($"Cannot create Photon connection to game." +
                $"{nameof(PhotonConnectionProvider)} can only connect to games created by {nameof(PhotonMatchmaker)}.");
        }
    }
}