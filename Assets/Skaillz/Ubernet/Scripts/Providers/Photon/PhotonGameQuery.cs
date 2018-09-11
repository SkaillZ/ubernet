namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// Query for joining a Photon game from a given room name.
    /// </summary>
    public class PhotonGameQuery : IGameQuery
    {
        /// <summary>
        /// Creates a new <see cref="PhotonGameQuery"/> from the given room name
        /// </summary>
        /// <param name="roomName">The name of the Photon room to join. The room must exist and not be full.</param>
        /// <returns></returns>
        public static PhotonGameQuery FromRoomName(string roomName)
        {
            return new PhotonGameQuery
            {
                RoomName = roomName
            };
        }
        
        /// <summary>
        /// The name of the Photon room to join. The room must exist and not be full.
        /// </summary>
        public string RoomName { get; set; }
        
        /// <summary>
        /// List of users to block a slot for. See Photon's docs about Slot reservation for more information.
        /// </summary>
        public string[] ExpectedPlayers { get; set; }
    }
}