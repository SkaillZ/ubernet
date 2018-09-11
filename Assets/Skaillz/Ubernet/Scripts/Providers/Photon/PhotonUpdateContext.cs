using System;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    /// <summary>
    /// Helper class that calls <see cref="PhotonMatchmaker.Update"/> automatically.
    /// </summary>
    public class PhotonUpdateContext : IDisposable
    {
        /// <summary>
        /// The default number of times per second the Update method is called
        /// </summary>
        public const int DefaultTickRate = 15;
        
        private readonly PhotonMatchmaker _matchmaker;
        private PhotonSettings _settings;
        
        private double _tickRate = DefaultTickRate;
        private bool _finished;
        
        internal PhotonUpdateContext(PhotonMatchmaker matchmaker)
        {
            _matchmaker = matchmaker;
            _settings = new PhotonSettings();
        }

        /// <summary>
        /// Stops the automatic updates
        /// </summary>
        public void Dispose()
        {
            _finished = true;
        }
        
        public PhotonUpdateContext WithSettings(PhotonSettings settings)
        {
            _settings = settings;
            return this;
        }

        public PhotonUpdateContext WithTickRate(double updatesPerSecond)
        {
            _tickRate = updatesPerSecond;
            return this;
        }

        public PhotonUpdateContext WithAppId(string appId)
        {
            _settings.AppId = appId;
            return this;
        }
        
        public PhotonUpdateContext WithAppVersion(string version)
        {
            _settings.AppVersion = version;
            return this;
        }
        
        public PhotonUpdateContext WithRegion(string region)
        {
            _settings.Region = region;
            return this;
        }
 
        /// <summary>
        /// Connects the <see cref="PhotonMatchmaker"/> to the cloud.
        /// </summary>
        /// <returns>An observable that resolves to the matchmaker once it has connected.</returns>
        public IObservable<PhotonMatchmaker> ConnectToCloud()
        {
            Observable.Interval(TimeSpan.FromSeconds(1 / _tickRate))
                .TakeWhile(_ => !_finished)
                .Subscribe(_ => _matchmaker.Update());
            
            return _matchmaker.ConnectToCloudWithSettings(_settings);
        }
    }
}