namespace Skaillz.Ubernet.Providers.Photon
{
    public class PhotonSettings
    {
        /// <summary>
        /// The Photon App ID
        /// </summary>
        public string AppId { get; set; }
        
        /// <summary>
        /// The version of your client. A new version also creates a new "virtual app" to separate players from older
        /// client versions.
        /// </summary>
        public string AppVersion { get; set; }
        
        /// <summary>
        /// The region to connect to. That region's Master Server is used to connect to Photon.
        /// </summary>
        public string Region { get; set; }
    }
}