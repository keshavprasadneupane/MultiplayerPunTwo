using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class RoomManager : MonoBehaviourPunCallbacks {

	[SerializeField] private Camera _mainCamera;

	[Space(4), Header("Player Spawn Settings")]
	[SerializeField] private Transform _spawnPoint;
	[SerializeField] private GameObject _playerPrefab;
	[SerializeField] private Color _player1Color = Color.red;
	[SerializeField] private Color _player2Color = Color.blue;

	[Space(4), Header("Ball Spawn Settings")]
	[SerializeField] private GameObject _ballPrefab;
	[SerializeField] private Transform _ballSpawnPoint1;
	[SerializeField] private Transform _ballSpawnPoint2;
	[SerializeField] private Color _ballColor1 = Color.white;
	[SerializeField] private Color _ballColor2 = Color.black;

	[SerializeField] private PhotonView _photonView;

	private const string ROOM_NAME = "TwoPlayerRoom";
	private const int MAX_PLAYERS = 2;
	private const bool IS_VISIBLE = true;

	private Spawner _spawner;
	private RestartCoordinator _restart;

	private void Awake() {
		if (_photonView == null)
			_photonView = GetComponent<PhotonView>();

		this._spawner = new Spawner(
			this._mainCamera, this._spawnPoint,
			this._playerPrefab, this._player1Color, this._player2Color,
			this._ballPrefab, this._ballSpawnPoint1, this._ballSpawnPoint2,
			this._ballColor1, this._ballColor2
		);
		this._restart = new RestartCoordinator(this._photonView, this._spawner);
	}

	private void Start() {
		PhotonNetwork.ConnectUsingSettings();
	}

	private void Update() {
		this._restart.HandleInput();
	}

	public override void OnConnectedToMaster() {
		base.OnConnectedToMaster();
		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby() {
		RoomOptions roomOptions = new() {
			MaxPlayers = MAX_PLAYERS,
			IsVisible = IS_VISIBLE,
			IsOpen = true
		};
		PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, roomOptions, TypedLobby.Default);
	}

	public override void OnJoinedRoom() {
		int playerIndex = PhotonNetwork.CurrentRoom.PlayerCount;
		bool isSecondPlayer = playerIndex == 2;
		Quaternion rotation = isSecondPlayer
			? Quaternion.Euler(0, 0, 180f)
			: Quaternion.identity;

		this._spawner.SpawnPlayer(rotation, isSecondPlayer);
		this._spawner.SpawnBall(rotation, isSecondPlayer);
		this._restart.SetSessionContext(playerIndex, rotation);
	}

	public override void OnJoinRoomFailed(short returnCode, string message) {
		Debug.LogError($"Failed to join room. Code: {returnCode}, Message: {message}");
	}

	public override void OnRoomPropertiesUpdate(Hashtable changedProps) {
		this._restart.OnRoomPropertiesUpdate(changedProps);
	}


	private class Spawner {
		private readonly Camera _mainCamera;
		private readonly Transform _spawnPoint;
		private readonly GameObject _playerPrefab;
		private readonly Color _player1Color;
		private readonly Color _player2Color;
		private readonly GameObject _ballPrefab;
		private readonly Transform _ballSpawnPoint1;
		private readonly Transform _ballSpawnPoint2;
		private readonly Color _ballColor1;
		private readonly Color _ballColor2;

		public Spawner(
			Camera mainCamera, Transform spawnPoint,
			GameObject playerPrefab, Color player1Color, Color player2Color,
			GameObject ballPrefab, Transform ballSpawnPoint1, Transform ballSpawnPoint2,
			Color ballColor1, Color ballColor2
		) {
			this._mainCamera = mainCamera;
			this._spawnPoint = spawnPoint;
			this._playerPrefab = playerPrefab;
			this._player1Color = player1Color;
			this._player2Color = player2Color;
			this._ballPrefab = ballPrefab;
			this._ballSpawnPoint1 = ballSpawnPoint1;
			this._ballSpawnPoint2 = ballSpawnPoint2;
			this._ballColor1 = ballColor1;
			this._ballColor2 = ballColor2;
		}

		public void SpawnPlayer(Quaternion rotation, bool isSecondPlayer) {
			Vector3 offset = new(Random.Range(-5f, 5f), Random.Range(-1f, 1f), 0f);
			var player = PhotonNetwork.Instantiate(
				this._playerPrefab.name,
				this._spawnPoint.position + offset,
				rotation
			);

			if (!player.TryGetComponent<InitManager>(out var mgr)) {
				Debug.LogError("InitManager not found on player prefab.");
				return;
			}

			mgr.LocalInitialize(isSecondPlayer ? this._player2Color : this._player1Color);
			if (isSecondPlayer)
				mgr.SetCalculationScaleForComponents(new Vector2(-1f, -1f));

			this._mainCamera.transform.rotation = rotation;
		}

		public void SpawnBall(Quaternion rotation, bool isSecondBall) {
			if (this._ballPrefab == null || this._ballSpawnPoint1 == null || this._ballSpawnPoint2 == null) {
				Debug.LogError("Ball prefab or spawn point not assigned.");
				return;
			}

			var ball = PhotonNetwork.Instantiate(
				this._ballPrefab.name,
				isSecondBall ? this._ballSpawnPoint2.position : this._ballSpawnPoint1.position,
				rotation
			);

			if (!ball.TryGetComponent<InitManager>(out var mgr)) {
				Debug.LogError("InitManager not found on ball prefab.");
				return;
			}

			mgr.LocalInitialize(isSecondBall ? this._ballColor2 : this._ballColor1);
			if (isSecondBall)
				mgr.SetCalculationScaleForComponents(new Vector2(-1f, -1f));
		}
	}

	private class RestartCoordinator {
		private const string READY_KEY = "RestartReady";

		private readonly PhotonView _photonView;
		private readonly Spawner _spawner;

		private int _playerIndexAtStart;
		private Quaternion _rotationAtStart;

		public RestartCoordinator(PhotonView photonView, Spawner spawner) {
			this._photonView = photonView;
			this._spawner = spawner;
		}

		public void SetSessionContext(int playerIndex, Quaternion rotation) {
			this._playerIndexAtStart = playerIndex;
			this._rotationAtStart = rotation;
		}

		public void HandleInput() {
			if (Input.GetKeyDown(KeyCode.R)) {
				Debug.Log("Restart requested by local player.");
				AddSelfToReadySet();
			}
		}

		public void OnRoomPropertiesUpdate(Hashtable changedProps) {
			if (!changedProps.ContainsKey(READY_KEY)) return;
			if (!PhotonNetwork.IsMasterClient) return;

			HashSet<int> ready = ParseReadySet();
			if (ready.Count >= PhotonNetwork.CurrentRoom.PlayerCount) {
				ClearReadySet();
				this._photonView.RPC(nameof(OnAllPlayersRequestedRestart), RpcTarget.All);
			}
		}

		public void RestartGame() {
			bool isSecondPlayer = this._playerIndexAtStart == 2;

			FindObjectsByType<PhotonView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
				.ToList()
				.ForEach(obj => {
					// this just to make sure the RoomManager itself isn't destroyed, since 
					// it's responsible for spawning the new objects
					if (obj != this._photonView && obj.IsMine)
						PhotonNetwork.Destroy(obj);
				});

			this._spawner.SpawnPlayer(this._rotationAtStart, isSecondPlayer);
			this._spawner.SpawnBall(this._rotationAtStart, isSecondPlayer);
		}

		private void AddSelfToReadySet() {
			HashSet<int> ready = ParseReadySet();
			ready.Add(PhotonNetwork.LocalPlayer.ActorNumber);
			Hashtable props = new() { { READY_KEY, SerializeReadySet(ready) } };
			PhotonNetwork.CurrentRoom.SetCustomProperties(props);
		}

		private void ClearReadySet() {
			Hashtable props = new() { { READY_KEY, "" } };
			PhotonNetwork.CurrentRoom.SetCustomProperties(props);
		}

		private HashSet<int> ParseReadySet() {
			var props = PhotonNetwork.CurrentRoom.CustomProperties;
			if (!props.TryGetValue(READY_KEY, out object raw) || raw is not string str || str == "")
				return new HashSet<int>();

			var set = new HashSet<int>();
			foreach (var part in str.Split(','))
				if (int.TryParse(part, out int id))
					set.Add(id);
			return set;
		}

		private string SerializeReadySet(HashSet<int> set) => string.Join(",", set);
	}


	[PunRPC]
	public void OnAllPlayersRequestedRestart() {
		Debug.Log($"[{(PhotonNetwork.IsMasterClient ? "Master" : "Client")}] All players confirmed restart.");
		this._restart.RestartGame();
	}
}