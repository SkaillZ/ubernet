using ExitGames.Client.Photon;
using Photon.Realtime;

namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// Options for joining random Photon games
    /// </summary>
    public class PhotonJoinRandomGameOptions : IJoinRandomGameOptions
    {
        /// <summary>
        /// Used to join any Photon game without limitations.
        /// </summary>
        public static PhotonJoinRandomGameOptions Unfiltered = new PhotonJoinRandomGameOptions();
        
        /// <summary>
        /// If present, only rooms with the given custom properties can be joined (see <see cref="RoomInfo.CustomProperties"/>)
        /// </summary>
        public Hashtable ExpectedCustomRoomProperties { get; set; }
        
        /// <summary>
        /// The expected maximum player number of the Photon room. Use 0 to accept any number of players.
        /// </summary>
        public byte ExpectedMaxPlayers { get; set; }
        
        /// <summary>
        /// Selects one of the available matchmaking algorithms. See <see cref="MatchmakingMode"/> enum for options.
        /// </summary>
        public MatchmakingMode MatchmakingMode { get; set; } = MatchmakingMode.FillRoom;
        
        /// <summary>
        /// The lobby in which to find a room.
        /// </summary>
        public TypedLobby Lobby { get; set; } = TypedLobby.Default;
        
        /// <summary>
        /// The "where" clause of an SQL statement used with Photon SQL lobbies. Refer to Photon's docs for more information.
        /// </summary>
        public string SqlLobbyFilter { get; set; }
        
        /// <summary>
        /// List of users to block a slot for. See Photon's docs about Slot reservation for more information.
        /// </summary>
        public string[] ExpectedUsers { get; set; }
    }
}