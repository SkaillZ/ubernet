using System;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Skaillz.Ubernet.DefaultSerializers.Unity;
using Skaillz.Ubernet.Providers.LiteNetLibExperimental;
using Skaillz.Ubernet.Providers.Photon;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
	public class MatchmakerUI : MonoBehaviour
	{
		private const int LoadSceneEventCode = 255;
		private const int RecoveryTimeout = 5000;

		private enum State
		{
			Uninitialized,
			CreatingMatchmaker,
			Matchmaking,
			JoiningGame,
			InGame,
			InGameWithEntityManager,
			DisconnectingFromGame,
			ErrorRollback,
			CreatingEntityManager,
			DisconnectingFromMatchmaker
		}

		public Type PlayerType
		{
			get => Type.GetType(_playerType);
			set => _playerType = value.AssemblyQualifiedName;
		}

		public bool AutoLoadScenes { get; set; } = true;
		public IObservable<string> OnLoadLevel => _loadLevelSubject.AsObservable();
		public NetworkEntityManager EntityManager => _entityManager;
		public IMatchmaker Matchmaker => _matchmaker;
		public IConnection Connection => _connection;

		[SerializeField] private string _lobbyScene;
		[SerializeField] private string _gameScene;
		[SerializeField] private bool _persistBetweenScenes = true;

		// Network entities settings
		[SerializeField] private bool _automaticallyCreateEntityManager = true;
		[SerializeField] private string _playerType = typeof(DefaultPlayer).AssemblyQualifiedName;
		[SerializeField] private int _serializationRate = 20;

		// Photon settings
		[SerializeField] private ConnectionProtocol _photonProtocol;
		[SerializeField] private string _photonRegion;
		[SerializeField] private string _photonAppId;
		[SerializeField] private string _photonAppVersion = "0.1";
		[SerializeField] private float _photonTickRate = 20;

		private IMatchmaker _matchmaker;
		private IConnection _connection;
		private NetworkEntityManager _entityManager;
		private State _state = State.Uninitialized;
		private string _photonRoomName;
		private string _liteNetLibAddress = "localhost";

		private readonly Subject<string> _loadLevelSubject = new Subject<string>();
		private Exception _exception;

		private readonly Rect _screenRect = new Rect(0, 0, 300, Screen.height);

		private void Awake()
		{
			if (FindObjectsOfType<MatchmakerUI>().Length > 1)
			{
				Destroy(gameObject);
			}

			if (_persistBetweenScenes)
			{
				DontDestroyOnLoad(this);
			}
		}

		private void OnGUI()
		{
			GUILayout.BeginArea(_screenRect);

			if (_exception != null)
			{
				GUILayout.Label($"An error occured: {_exception.Message}\n\n" +
				                $"Please refer to the console for more information.");

				if (GUILayout.Button("Close and try to recover"))
				{
					_exception = null;
					Rollback();
				}
			}
			else
				switch (_state)
				{
					case State.Uninitialized:
						GUILayout.Label("Not connected. Choose a matchmaker to connect to:");
						if (GUILayout.Button("Photon"))
						{
							HandleErrors(CreatePhotonMatchmaker());
						}
						
						GUILayout.Space(10f);
						
						if (GUILayout.Button("LiteNetLib Server"))
						{
							HandleErrors(CreateLiteNetLibServer());
						}

						GUILayout.BeginHorizontal();
						_liteNetLibAddress = GUILayout.TextField(_liteNetLibAddress);
						
						if (GUILayout.Button("LiteNetLib Client"))
						{
							HandleErrors(CreateLiteNetLibClient());
						}
						GUILayout.EndHorizontal();
						break;
					case State.ErrorRollback:
						GUILayout.Label("Attempting to recover...");
						break;
					case State.CreatingMatchmaker:
						GUILayout.Label("Creating the matchmaker...");
						break;
					case State.Matchmaking:
					{
						GUILayout.Label($"Connected to matchmaker {_matchmaker?.GetType().Name}.");
						if (GUILayout.Button("Create Game"))
						{
							HandleErrors(CreateGame());
						}

						if (GUILayout.Button("Join Random Game"))
						{
							HandleErrors(JoinRandomGame());
						}

						if (_matchmaker is PhotonMatchmaker)
						{
							GUILayout.BeginHorizontal();
							if (GUILayout.Button("Join Photon Room"))
							{
								HandleErrors(JoinNamedPhotonRoom());
							}

							_photonRoomName = GUILayout.TextField(_photonRoomName);
							GUILayout.EndHorizontal();
						}
						
						GUILayout.Space(10f);

						if (_matchmaker is IDisconnectable<DisconnectReason>)
						{
							if (GUILayout.Button("Disconnect From Matchmaker"))
							{
								HandleErrors(DisconnectFromMatchmaker());
							}
						}

						break;
					}
					case State.JoiningGame:
						GUILayout.Label("Creating/Joining a game...");
						break;
					case State.InGame:
					case State.CreatingEntityManager:
						GUILayout.Label($"Connected to: {_connection.GetType().Name}");

						var clients = _connection.Clients;
						GUILayout.Label($"{clients.Count} clients are connected:");

						for (var i = 0; i < clients.Count; i++)
						{
							var client = clients[i];
							GUILayout.Label($"Client #{i + 1}: {client}");
						}

						if (GUILayout.Button("Disconnect"))
						{
							HandleErrors(DisconnectFromGame());
						}

						if (_state == State.CreatingEntityManager)
						{
							GUILayout.Label("Creating Entity Manager...");
						}
						else if (GUILayout.Button("Create Entity Manager For Current Connection") ||
						         _automaticallyCreateEntityManager)
						{
							HandleErrors(CreateEntityManager());
						}

						if (!_connection.IsConnected)
						{
							_state = State.Matchmaking;
						}

						break;
					case State.InGameWithEntityManager:
						GUILayout.Label($"Connected to: {_connection.GetType().Name} with EntityManager");

						var players = _entityManager.Players;
						GUILayout.Label($"{players.Count} players are connected:");

						for (var i = 0; i < players.Count; i++)
						{
							var player = players[i];
							GUILayout.Label($"Player #{i + 1}: {player}");
						}

						if (GUILayout.Button("Disconnect"))
						{
							_entityManager = null;
							HandleErrors(DisconnectFromGame());
						}

						if (!string.IsNullOrEmpty(_gameScene) && SceneManager.GetActiveScene().name != _gameScene)
						{
							if (GUILayout.Button($"Start Game (loads '{_gameScene}')"))
							{
								LoadScene(_gameScene, true);
							}
						}

						if (!_connection.IsConnected)
						{
							_state = State.Matchmaking;
						}

						break;
					case State.DisconnectingFromGame:
						GUILayout.Label($"Disconnecting from {_connection.GetType().Name}...");
						break;
					case State.DisconnectingFromMatchmaker:
						GUILayout.Label($"Disconnecting from {_matchmaker.GetType().Name}...");
						break;
				}

			GUILayout.EndArea();
		}

		private async Task CreatePhotonMatchmaker()
		{
			_state = State.CreatingMatchmaker;
			
			_matchmaker = await PhotonMatchmaker.NewContext()
				.WithAppId(_photonAppId)
				.WithProtocol(_photonProtocol)
				.WithRegion(_photonRegion)
				.WithAppVersion(_photonAppVersion)
				.WithTickRate(_photonTickRate)
				.ConnectToCloud();

			_state = State.Matchmaking;
		}

		private async Task CreateGame()
		{
			_state = State.JoiningGame;
			var game = await _matchmaker.CreateGame(_matchmaker is PhotonMatchmaker
				? new PhotonCreateGameOptions()
				: null);
			await ConnectToGame(game);

			_state = State.InGame;
		}

		private async Task JoinRandomGame()
		{
			_state = State.JoiningGame;
			var game = await _matchmaker.FindRandomGame(_matchmaker is PhotonMatchmaker
				? new PhotonJoinRandomGameOptions()
				: null);
			await ConnectToGame(game);
			_state = State.InGame;
		}

		private async Task JoinNamedPhotonRoom()
		{
			_state = State.JoiningGame;
			var game = await _matchmaker.FindGame(new PhotonGameQuery
			{
				RoomName = _photonRoomName
			});
			await ConnectToGame(game);
			_state = State.InGame;
		}
		
		private async Task CreateLiteNetLibServer()
		{
			_state = State.JoiningGame;
			await Observable.NextFrame();
			
			_connection = LiteNetLibConnectionExperimental.CreateServer(5000, 2, "0.1", false);
			_connection.RegisterUnityDefaultTypes();
			_connection.AutoUpdate();
			
			_connection.OnEvent(LoadSceneEventCode)
				.Subscribe(evt =>
				{
					LoadScene((string) evt.Data, false);
				});
			
			_state = State.InGame;
		}
		
		private async Task CreateLiteNetLibClient()
		{
			_state = State.JoiningGame;
			await Observable.NextFrame();
			
			_connection = LiteNetLibConnectionExperimental.CreateClient(_liteNetLibAddress, 5000, 2, "0.1", false);
			_connection.RegisterUnityDefaultTypes();
			_connection.AutoUpdate();
			
			_connection.OnEvent(LoadSceneEventCode)
				.Subscribe(evt =>
				{
					LoadScene((string) evt.Data, false);
				});
			
			_state = State.InGame;
		}

		private async Task CreateEntityManager()
		{
			_state = State.CreatingEntityManager;
			_entityManager = await _connection.CreateEntityManager(_serializationRate)
				.SetLocalPlayer((IPlayer) Activator.CreateInstance(Type.GetType(_playerType)))
				.SetAsDefaultEntityManager()
				.Initialize();
			_state = State.InGameWithEntityManager;
		}

		private async Task ConnectToGame(IGame game)
		{
			if (game is PhotonAlreadyJoinedGame)
			{
				_connection = (await game.ConnectWithPhoton()).AutoUpdate(_photonTickRate);
			}
			
			_connection.RegisterUnityDefaultTypes();
			_connection.OnDisconnected
				.TakeWhile(_ => _connection != null)
				.Subscribe(_ =>
				{
					Debug.LogWarning("Disconnected");
					
					if (!string.IsNullOrEmpty(_lobbyScene) && SceneManager.GetActiveScene().name != _lobbyScene)
					{
						SceneManager.LoadScene(_lobbyScene);
					}
				});

			_connection.OnEvent(LoadSceneEventCode)
				.Subscribe(evt =>
				{
					LoadScene((string) evt.Data, false);
				});
		}
		
		private async Task DisconnectFromMatchmaker()
		{
			_state = State.DisconnectingFromMatchmaker;
			await ((IDisconnectable<DisconnectReason>)_matchmaker).Disconnect();

			_state = State.Uninitialized;
			_matchmaker = null;
		}

		private async Task DisconnectFromGame()
		{
			_state = State.DisconnectingFromGame;
			await _connection.Disconnect();

			_state = _matchmaker != null ? State.Matchmaking : State.Uninitialized;
			_connection = null;
		}

		private void LoadScene(string sceneName, bool send)
		{
			Debug.Log($"Loading scene: {sceneName}");
			
			if (send)
			{
				_entityManager.SendEvent(LoadSceneEventCode, sceneName);
			}

			_loadLevelSubject.OnNext(sceneName);
			if (AutoLoadScenes)
			{
				_entityManager.Connection.SendEvents = false;

				Debug.Log("LoadScene start");
				SceneManager.LoadScene(sceneName);
				Debug.Log($"Loaded scene: {sceneName}");
				_entityManager.Connection.SendEvents = true;
			}
		}

		private async void HandleErrors(Task operation)
		{
			try
			{
				await operation;
			}
			catch (Exception e)
			{
				_exception = e;
				Debug.LogException(e);
			}
		}

		private async void Rollback()
		{
			_state = State.ErrorRollback;
			try
			{
				if (_connection != null)
				{
					await _connection.Disconnect().Timeout(TimeSpan.FromMilliseconds(RecoveryTimeout));
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			_connection = null;
			_state = State.DisconnectingFromMatchmaker;

			try
			{
				if (_matchmaker is IDisconnectable<DisconnectReason>)
				{
					await ((IDisconnectable<DisconnectReason>) _matchmaker).Disconnect()
						.Timeout(TimeSpan.FromMilliseconds(RecoveryTimeout));
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			_state = State.Uninitialized;
			_matchmaker = null;
		}
	}
}
