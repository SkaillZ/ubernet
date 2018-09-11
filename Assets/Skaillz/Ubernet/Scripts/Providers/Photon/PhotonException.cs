using System;

namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// A generic Exception thrown when Photon operations fai with the Photon error code.
    /// </summary>
    public class PhotonException : Exception
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
        public PhotonException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}