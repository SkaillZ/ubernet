namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// Photon when joining a Photon room failed.
    /// </summary>
    public class PhotonGameJoinException : GameJoinException
    {
        /// <summary>
        /// The Photon <see cref="ExitGames.Client.Photon.LoadBalancing.ErrorCode"/>
        /// </summary>
        public int ErrorCode { get; private set; }
        
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="errorCode">The Photon <see cref="ExitGames.Client.Photon.LoadBalancing.ErrorCode"/></param>
        public PhotonGameJoinException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}