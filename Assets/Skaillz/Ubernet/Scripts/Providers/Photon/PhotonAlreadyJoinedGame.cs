using System;
 using ExitGames.Client.Photon.LoadBalancing;
 using UniRx;
 
 namespace Skaillz.Ubernet.Providers.Photon
 {
     /// <summary>
     /// The <see cref="IGame"/> representing the Photon <see cref="Room"/> the player is currently in.
     /// Call <see cref="GetConnection"/> to get a <see cref="PhotonRoomConnection"/> to the current room.
     /// </summary>
     ///
     /// Due to platform limitations, <see cref="PhotonMatchmaker.CreateGame"/> and <see cref="PhotonMatchmaker.FindGame"/>
     /// on <see cref="PhotonMatchmaker"/> connect to a Photon room immediately. Other platforms can return a <see cref="IGame"/>
     /// containing information about the game, whereas Photon can only do this for games in the lobby.
     public class PhotonAlreadyJoinedGame : IGame
     {
         /// <summary>
         /// The underlying Photon <see cref="LoadBalancingClient"/>
         /// </summary>
         public LoadBalancingClient PhotonClient { get; }
         
         /// <summary>
         /// Creates a new instance from a connected <see cref="LoadBalancingClient"/>
         /// </summary>
         /// <param name="photonClient">A <see cref="LoadBalancingClient"/> that is connected to a <see cref="Room"/></param>
         public PhotonAlreadyJoinedGame(LoadBalancingClient photonClient)
         {
             PhotonClient = photonClient;
         }
 
         /// <summary>
         /// Creates a <see cref="PhotonRoomConnection"/> to the current room
         /// </summary>
         /// <returns>A connection to the current Photon room</returns>
         public PhotonRoomConnection GetConnection()
         {
             return new PhotonRoomConnection(PhotonClient);
         }
     }
 }