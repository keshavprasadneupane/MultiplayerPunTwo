using Photon.Pun;
using UnityEngine;

public abstract class LocalInitializationBase : MonoBehaviour {
	public bool IsInitialized { get; private set; } = false;
	private readonly InitializationLogger _logger = new();
	private PhotonView _photonView;
	public string LogMessage => this._logger.ScenePath;


	protected virtual void Awake() {
		this.enabled = false;
	}

	protected virtual void Start() {
		// PUN2 defers Start to the frame after PhotonNetwork.Instantiate, so if
		// LocalInitialize was called from RoomManager.OnJoinedRoom (which runs during
		// the network callback), _photonView is guaranteed set before this executes.
		//
		// If _photonView is still null here, LocalInitialize was never called — meaning
		// Photon instantiated this object internally to represent a remote player.
		// In that case IsPhotonViewMine() returns false, so we disable the component
		// and run ViewNotMineLogic() to apply any remote-instance setup (e.g. kinematic
		// rigidbody, disabled input) without needing an explicit external call.
		if (!IsPhotonViewMine()) {
			this.enabled = false;
			ViewNotMineLogic();
		}
	}
	public void LocalInitialize(PhotonView photonView) {
		try {
			this._photonView = photonView;
			this._logger.CachePath(this);
			if (this.IsInitialized) return;
			this.enabled = true;
			this.IsInitialized = OnLocalInit();
		} catch (System.Exception ex) {
			this._logger.LogInitializationError(this, ex);
			this.enabled = false;
			this.IsInitialized = false;
			this._photonView = null;
		}
	}

	protected abstract bool OnLocalInit();
	protected virtual void ViewNotMineLogic() { }

	protected virtual void Update() {
		if (!this.IsInitialized) {
			this._logger.LogUninitializedUpdate(this);
			return;
		}
		OnUpdate();
	}

	protected virtual void OnUpdate() { }

	protected bool IsPhotonViewMine() {
		if (this._photonView == null) return true;
		return this._photonView.IsMine;
	}
}