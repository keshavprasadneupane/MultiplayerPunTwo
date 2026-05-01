using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class RoomManager : MonoBehaviourPunCallbacks {
	[SerializeField] Camera mainCamera;

	[Space(4), Header("Player Spawn Settings")]
	[SerializeField] Transform _spawnPoint;
	[SerializeField] GameObject _playerPrefab;
	[SerializeField] Color player1Color = Color.red;
	[SerializeField] Color player2Color = Color.blue;


	[Space(4), Header("Ball Spawn Settings")]
	[SerializeField] GameObject _ballPrefab;
	[SerializeField] Transform _ballSpawnPoint1;
	[SerializeField] Transform _ballSpawnPoint2;
	[SerializeField] Color ballColor1 = Color.white;
	[SerializeField] Color ballColor2 = Color.black;

	private const string roomName = "TwoPlayerRoom";
	private const int maxPlayers = 2;
	private const bool isVisible = true;


	void Start() {
		Debug.Log("RoomManager Start");
		PhotonNetwork.ConnectUsingSettings();
	}
	public override void OnConnectedToMaster() {
		base.OnConnectedToMaster();
		PhotonNetwork.JoinLobby();
	}


	public override void OnJoinedLobby() {
		RoomOptions roomOptions = new() {
			MaxPlayers = maxPlayers,
			IsVisible = isVisible,
			IsOpen = true
		};
		PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
	}

	public override void OnJoinedRoom() {
		int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
		bool isSecondPlayer = playerCount == 2;
		Quaternion spawnRotation = isSecondPlayer
			? Quaternion.Euler(0, 0, 180f)
			: Quaternion.identity;

		Color playerColor = (playerCount == 1) ? this.player1Color : this.player2Color;
		SpawnPlayer(spawnRotation, playerColor, isSecondPlayer);

		SpawnBall(spawnRotation, isSecondPlayer);

	}


	public override void OnJoinRoomFailed(short returnCode, string message) {
		Debug.LogError($"Failed to join the room. Return code: {returnCode}, Message: {message}");
	}



	private void SpawnPlayer(Quaternion spawnRotation, Color playerColor, bool isSecondPlayer) {
		Vector3 randomUnityPosition = new(Random.Range(-5f, 5f), Random.Range(-1f, 1f), 0f);
		/*
			rotation around Z axis about 180* is same as flipping the local scale on both X and Y axes. 
			This is a simple way to ensure that the second player spawns facing the opposite 
			direction without needing to modify the player prefab's local scale, 
			which could have unintended consequences on physics and movement calculations. 
			and also hamper in network synchronization of the player object.
		*/
		var _player = PhotonNetwork.Instantiate(
			this._playerPrefab.name,
			this._spawnPoint.position + randomUnityPosition,
			spawnRotation
		);

		if (!_player.TryGetComponent<InitManager>(out var mgr)) {
			Debug.LogError("InitManager component not found on player prefab.");
			return;
		}

		mgr.LocalInitialize(playerColor);
		if (isSecondPlayer) {
			mgr.SetCalculationScaleForComponents(new Vector2(-1f, -1f));
		}

		this.mainCamera.gameObject.transform.rotation = spawnRotation;
	}
	private void SpawnBall(Quaternion spawnRotation, bool isSecondBall) {
		if (this._ballPrefab == null || this._ballSpawnPoint1 == null
			|| this._ballSpawnPoint2 == null) {
			Debug.LogError("Ball prefab or spawn point is not assigned in the inspector.");
			return;
		}
		var ball = PhotonNetwork.Instantiate(
			this._ballPrefab.name,
			isSecondBall ? this._ballSpawnPoint2.position : this._ballSpawnPoint1.position,
			spawnRotation
		);
		if (!ball.TryGetComponent<InitManager>(out var ballController)) {
			Debug.LogError("InitManager component not found on ball prefab.");
			return;
		}
		Debug.Log($"SpawnBall called with isSecondBall: {isSecondBall}");
		ballController.LocalInitialize(
			isSecondBall ? this.ballColor2 : this.ballColor1
		);
		if (isSecondBall) {
			ballController.SetCalculationScaleForComponents(new Vector2(-1f, -1f));
		}
	}
}