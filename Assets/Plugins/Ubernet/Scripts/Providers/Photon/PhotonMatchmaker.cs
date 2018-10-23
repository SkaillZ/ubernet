using System;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    public class PhotonMatchmaker : IMatchmaker, IUpdateable, IDisconnectable<DisconnectReason>
    {
        private LoadBalancingClient _photonClient;
        private PhotonSettings _photonSettings;
        
        private readonly ISubject<DisconnectReason> _disconnectedSubject = new Subject<DisconnectReason>();

        /// <summary>
        /// The current state of connection. Rooms can only be created or joined if the state is <see cref="ConnectionState.Connected"/>
        /// </summary>
        public virtual ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        /// <summary>
        /// The Photon LoadBalancingClient used by the matchmaker
        /// </summary>
        public virtual LoadBalancingClient PhotonClient
        {
            get { return _photonClient; }
            internal set
            {
                _photonClient = value;
                InitializeEvents();
            }
        }

        /// <summary>
        /// Calls subscriptions when disconnected from Photon
        /// </summary>
        public virtual IObservable<DisconnectReason> OnDisconnected => _disconnectedSubject.AsObservable();

        /// <summary>
        /// Creates a new update context for Photon. It is recommended to configure Photon matchmakers through an update context.
        /// </summary>
        /// <returns>An update context that can is used to configure the Matchmaker</returns>
        public static PhotonUpdateContext NewContext()
        {
            return new PhotonUpdateContext(new PhotonMatchmaker());
        }

        /// <summary>
        /// Creates a new matchmaker with the given Photon <see cref="LoadBalancingClient"/>
        /// </summary>
        /// Since you have to call <see cref="Update"/> manually, it is recommended to create an instance with
        /// <see cref="NewContext"/> instead
        /// <param name="loadBalancingClient">The Photon client</param>
        public PhotonMatchmaker(LoadBalancingClient loadBalancingClient = null)
        {
            _photonClient = loadBalancingClient ?? new LoadBalancingClient();
            InitializeEvents();
        }
        
        /// <summary>
        /// Connects to the Photon cloud with the given settings.
        /// </summary>
        /// Since you have to call <see cref="Update"/> manually, it is recommended to create an instance with
        /// <see cref="NewContext"/> instead
        /// 
        /// <param name="settings">The settings for connecting</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If <see cref="settings"/> is null</exception>
        /// <exception cref="ArgumentException">If properties on the settings are missing</exception>
        public IObservable<PhotonMatchmaker> ConnectToCloudWithSettings(PhotonSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            
            if (string.IsNullOrEmpty(settings.AppId))
            {
                throw new ArgumentException($"{nameof(settings.AppId)} on {nameof(PhotonSettings)} must not be null or empty.");
            }
            if (string.IsNullOrEmpty(settings.Region))
            {
                throw new ArgumentException($"{nameof(settings.Region)} on {nameof(PhotonSettings)} must not be null or empty.");
            }
            if (string.IsNullOrEmpty(settings.AppVersion))
            {
                throw new ArgumentException($"{nameof(settings.AppVersion)} on {nameof(PhotonSettings)} must not be null or empty.");
            }

            _photonClient.AutoJoinLobby = false;
            _photonClient.AppId = settings.AppId;
            _photonClient.AppVersion = settings.AppVersion;
            _photonClient.TransportProtocol = settings.Protocol;

            _photonSettings = settings;

            return Connect();
        }

        /// <summary>
        /// Disconnects from Photon.
        /// </summary>
        /// <returns>An observable that resolves with a <see cref="DisconnectReason"/> after the matchmaker was disconnected</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual IObservable<DisconnectReason> Disconnect()
        {
            State = ConnectionState.Disconnecting;
            if (!_photonClient.IsConnected)
            {
                throw new InvalidOperationException("Cannot disconnect from Photon: Not connected");
            }

            var cancel = Observable.FromEvent<OperationResponse>(
                    handler => _photonClient.OnOpResponseAction += handler,
                    handler => _photonClient.OnOpResponseAction -= handler
                )
                .Where(response => response.ReturnCode != 0)
                .First()
                .Subscribe(response =>
                {
                    _disconnectedSubject.OnError(new ConnectionException("Cannot disconnect from Photon.", DisconnectReason.Exception));
                });

            _photonClient.Disconnect();
            return _disconnectedSubject.AsObservable().First().Select(c =>
            {
                cancel.Dispose();
                return c;
            });
        }

        /// <summary>
        /// Creates a new Photon room with the given options. The matchmaker's state must be <see cref="ConnectionState.Connected"/>.
        /// </summary>
        /// See <see cref="LoadBalancingClient.OpCreateRoom"/> for more information.
        ///
        /// <inheritdoc cref="IMatchmaker.CreateGame"/>
        /// <param name="options">The <see cref="PhotonCreateGameOptions"/> used to create the room</param>
        /// <returns>An observable resolving with the <see cref="PhotonAlreadyJoinedGame"/> that was joined with the given query.</returns>
        /// <exception cref="ArgumentException">If <see cref="options"/> is not of type <see cref="PhotonCreateGameOptions"/></exception>
        public virtual IObservable<IGame> CreateGame(ICreateGameOptions options)
        {
            var photonOptions = options as PhotonCreateGameOptions;
            if (photonOptions == null)
            {
                throw new ArgumentException($"{nameof(options)} must be of type {nameof(PhotonCreateGameOptions)}.", nameof(options));
            }
            
            if (_photonClient.State != ClientState.ConnectedToMasterserver && _photonClient.State != ClientState.JoinedLobby)
            {
                return Observable.Throw<IGame>(new InvalidOperationException($"Operation failed: Invalid state '{_photonClient.State}'." +
                    " Please make sure you are connected to Photon."));
            }
            
            var observable = PhotonUtils.CreateObservableForExpectedStateChange<IGame>(_photonClient,
                expectedState: ClientState.Joined, returnValue: new PhotonAlreadyJoinedGame(_photonClient));
            
            _photonClient.OpCreateRoom(photonOptions.RoomName, photonOptions, photonOptions.PhotonLobby, photonOptions.ExpectedUsers);
            State = ConnectionState.JoiningRoom;

            return observable;
        }
        
        /// <summary>
        /// Join a room found by the given query. The matchmaker's state must be <see cref="ConnectionState.Connected"/>.
        /// </summary>
        /// Note that Photon will be connected to any found room instantly.
        /// See <see cref="LoadBalancingClient.OpJoinRoom"/> for more information.
        ///
        /// <inheritdoc cref="IMatchmaker.FindGame"/>
        /// <param name="options">The <see cref="PhotonGameQuery"/> used to find a game</param>
        /// <returns>An observable resolving with the <see cref="PhotonAlreadyJoinedGame"/> that was joined with the given query.</returns>
        /// <exception cref="ArgumentException">If <see cref="query"/> is not of type <see cref="PhotonGameQuery"/></exception>
        public virtual IObservable<IGame> FindGame(IGameQuery query)
        {
            var photonOptions = query as PhotonGameQuery;
            if (photonOptions == null)
            {
                throw new ArgumentException($"{nameof(query)} must be of type {nameof(PhotonGameQuery)}.", nameof(query));
            }
            
            if (string.IsNullOrEmpty(photonOptions.RoomName))
            {
                throw new ArgumentException($"{nameof(photonOptions.RoomName)} must not be null or empty.", nameof(query));
            }
            
            if (_photonClient.State != ClientState.ConnectedToMasterserver && _photonClient.State != ClientState.JoinedLobby)
            {
                return Observable.Throw<IGame>(new InvalidOperationException($"Operation failed: Invalid state '{_photonClient.State}'." +
                   " Please make sure you are connected to Photon."));
            }
            
            var observable = PhotonUtils.CreateObservableForExpectedStateChange<IGame>(_photonClient,
                expectedState: ClientState.Joined, returnValue: new PhotonAlreadyJoinedGame(_photonClient));
            State = ConnectionState.JoiningRoom;

            _photonClient.OpJoinRoom(photonOptions.RoomName, photonOptions.ExpectedPlayers);

            return observable;
        }
        
        /// <summary>
        /// Joins a random Photon room that matches with the given options. The matchmaker's state must be <see cref="ConnectionState.Connected"/>.
        /// </summary>
        /// Note that Photon will be connected to any found room instantly.
        /// See <see cref="LoadBalancingClient.OpJoinRoom"/> for more information.
        ///
        /// <inheritdoc cref="IMatchmaker.FindRandomGame"/>
        /// <param name="options">The <see cref="PhotonJoinRandomGameOptions"/> used to find a random game</param>
        /// <returns>An observable resolving with the <see cref="PhotonAlreadyJoinedGame"/> that was joined.</returns>
        /// <exception cref="ArgumentException">If <see cref="options"/> is not of type <see cref="PhotonJoinRandomGameOptions"/></exception>
        public virtual IObservable<IGame> FindRandomGame(IJoinRandomGameOptions options)
        {
            var photonOptions = options as PhotonJoinRandomGameOptions;
            if (photonOptions == null)
            {
                throw new ArgumentException($"{nameof(options)} must be of type {nameof(PhotonJoinRandomGameOptions)}.", nameof(options));
            }
            
            if (_photonClient.State != ClientState.ConnectedToMasterserver && _photonClient.State != ClientState.JoinedLobby)
            {
                return Observable.Throw<IGame>(new InvalidOperationException($"Operation failed: Invalid state '{_photonClient.State}'." +
                    " Please make sure you are connected to Photon."));
            }
            
            var observable = PhotonUtils.CreateObservableForExpectedStateChange<IGame>(_photonClient,
                expectedState: ClientState.Joined, returnValue: new PhotonAlreadyJoinedGame(_photonClient));
            State = ConnectionState.JoiningRoom;

            _photonClient.OpJoinRandomRoom(photonOptions.ExpectedCustomRoomProperties, photonOptions.ExpectedMaxPlayers, 
                photonOptions.MatchmakingMode, photonOptions.Lobby, photonOptions.SqlLobbyFilter, photonOptions.ExpectedUsers);

            return observable;
        }

        /// <summary>
        /// Sends and receives messages by calling <see cref="LoadBalancingClient.Service"/> on the Photon client.
        /// <see cref="PhotonRoomConnection.Update"/> must be called on the connection while in-game.
        /// </summary>
        public virtual void Update()
        {
            if (State != ConnectionState.Disconnected && _photonClient.State != ClientState.Joined)
            {
                _photonClient.Service();
            }
        }

        protected virtual IObservable<PhotonMatchmaker> Connect()
        {
            if (_photonClient.State != ClientState.PeerCreated && _photonClient.State != ClientState.Disconnected)
            {
                return Observable.Throw<PhotonMatchmaker>(new InvalidOperationException($"Operation failed: Invalid state '{_photonClient.State}'." +
                    " Please make sure you aren't already connected to Photon."));
            }
            
            var observable = PhotonUtils.CreateObservableForExpectedStateChange(_photonClient,
                expectedState: ClientState.ConnectedToMasterserver, returnValue: this);
            State = ConnectionState.Connecting;
            
            if (!_photonClient.ConnectToRegionMaster(_photonSettings.Region))
            {
                return Observable.Throw<PhotonMatchmaker>(new ConnectionException("Cannot connect to Photon.",
                    DisconnectReason.Exception));
            }

            return observable;
        }

        private void InitializeEvents()
        {
            _photonClient.OnStateChangeAction += state =>
            {
                switch (state)
                {
                    case ClientState.Disconnecting:
                        State = ConnectionState.Disconnecting;
                        break;
                    case ClientState.Disconnected:
                        var reason = PhotonUtils.ConvertPhotonDisconnectCause(_photonClient.DisconnectedCause);
                        _disconnectedSubject.OnNext(reason);
                        State = ConnectionState.Disconnected;
                        break;
                }
            };
        }
    }
}