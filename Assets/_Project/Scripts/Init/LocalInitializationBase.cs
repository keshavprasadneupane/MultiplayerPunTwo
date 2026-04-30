using Photon.Pun;
using UnityEngine;

public abstract class LocalInitializationBase : MonoBehaviour
{
	public bool IsInitialized { get; private set; } = false;
	private InitializationLogger _logger;

	public string LogMessage => _logger.ScenePath;

	public void LocalInitialize(PhotonView photonView)
	{
		try
		{
			this.enabled = true;
			this._logger.CachePath(this);
			if (this.IsInitialized) return;
			this.IsInitialized = OnLocalInit(photonView);
		}
		catch (System.Exception ex)
		{
			this._logger.LogInitializationError(this, ex);
			this.enabled = false;
			this.IsInitialized = false;
		}
	}

	protected abstract bool OnLocalInit(PhotonView photonView);

	public void Update()
	{
		if (!this.IsInitialized)
		{
			this._logger.LogUninitializedUpdate(this);
			return;
		}
		OnUpdate();
	}

	protected virtual void OnUpdate() { }
}