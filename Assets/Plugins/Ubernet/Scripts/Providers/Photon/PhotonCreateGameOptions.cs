using ExitGames.Client.Photon.LoadBalancing;

namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// Options for creating a Photon room (see <see cref="RoomOptions"/>).
    /// </summary>
    public class PhotonCreateGameOptions : RoomOptions, ICreateGameOptions
    {
        /// <summary>
        /// Creates a new <see cref="PhotonCreateGameOptions"/> object from a given Photon room name
        /// </summary>
        /// <param name="roomName">The name of the newly created room</param>
        /// <returns></returns>
        public static PhotonCreateGameOptions FromRoomName(string roomName)
        {
            return new PhotonCreateGameOptions
            {
                RoomName = roomName
            };
        }
        
        /// <summary>
        /// The name of the new Photon room. This name must be unique. If null, a GUID is assigned as the name automatically.
        /// </summary>
        public string RoomName { get; set; }
        
        /// <summary>
        /// The Photon lobby to use
        /// </summary>
        public TypedLobby PhotonLobby { get; set; } = TypedLobby.Default;
        
        /// <summary>
        /// Optional list of users (by UserId) who are expected to join this game and who you want to block a slot for.
        /// </summary>
        /// See Photon's docs about Slot Reservations for more information.
        public string[] ExpectedUsers { get; set; }
    }
}