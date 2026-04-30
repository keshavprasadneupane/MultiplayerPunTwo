using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class RoomManager : MonoBehaviourPunCallbacks
{
	[SerializeField] Camera mainCamera;
	[SerializeField] GameObject _playerPrefab;
	[SerializeField] Transform _spawnPoint;
	[SerializeField] Color player1Color = Color.red;
	[SerializeField] Color player2Color = Color.blue;

	private const string roomName = "TwoPlayerRoom";
	private const int maxPlayers = 2;
	private const bool isVisible = true;


	void Start()
	{
		Debug.Log("RoomManager Start");
		PhotonNetwork.ConnectUsingSettings();
	}
	public override void OnConnectedToMaster()
	{
		base.OnConnectedToMaster();
		PhotonNetwork.JoinLobby();
	}


	public override void OnJoinedLobby()
	{
		RoomOptions roomOptions = new()
		{
			MaxPlayers = maxPlayers,
			IsVisible = isVisible,
			IsOpen = true
		};
		PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
	}

	public override void OnJoinedRoom()
	{
		int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
		Vector3 randomUnityPosition = new(Random.Range(-5f, 5f), Random.Range(-1f, 1f), 0f);

		/*
			rotation around Z axis about 180* is same as flipping the local scale on both X and Y axes. 
			This is a simple way to ensure that the second player spawns facing the opposite 
			direction without needing to modify the player prefab's local scale, 
			which could have unintended consequences on physics and movement calculations. 
			and also hamper in network synchronization of the player object.
		*/
		Quaternion spawnRotation = (playerCount == 2)
			? Quaternion.Euler(0, 0, 180f)
			: Quaternion.identity;

		Color playerColor = (playerCount == 1) ? this.player1Color : this.player2Color;

		var _player = PhotonNetwork.Instantiate(
			this._playerPrefab.name,
			this._spawnPoint.position + randomUnityPosition,
			spawnRotation
		);

		if (!_player.TryGetComponent<InitManager>(out var mgr))
		{
			Debug.LogError("InitManager component not found on player prefab.");
			return;
		}

		mgr.LocalInitialize(playerColor);

		if (playerCount == 2)
		{
			mgr.SetCalculationScaleForComponents(new Vector2(-1f, -1f));
		}
		this.mainCamera.gameObject.transform.rotation = spawnRotation;
	}
	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.LogError($"Failed to join the room. Return code: {returnCode}, Message: {message}");
	}
}