using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class InitManager : MonoBehaviourPun
{
	[SerializeField] private PhotonView _photonView;
	[SerializeField] SpriteRenderer _spriteRenderer;
	[SerializeField] List<LocalInitializationBase> initializables;

	private readonly List<IOrientable2D> orientable2d = new();

	public void SetCalculationScaleForComponents(Vector2 localScaleSign)
	{
		foreach (var component in this.orientable2d)
			component.SetCalculationScale(localScaleSign);
	}

	public void LocalInitialize(Color playerColor)
	{
		if (this._photonView == null)
		{
			Debug.LogError("PhotonView reference is missing in InitManager.");
			return;
		}

		// RPC over IPunObservable: color is set once at spawn, not per-frame state.
		// AllBuffered ensures late joiners still receive it from the server cache.
		this._photonView.RPC(
			nameof(SyncColor),
			RpcTarget.AllBuffered,
			playerColor.r, playerColor.g, playerColor.b, playerColor.a
		);

		foreach (var initializable in this.initializables)
		{
			if (initializable == null) continue;
			initializable.LocalInitialize(this._photonView);
			if (initializable is IOrientable2D injectScaleSign)
				this.orientable2d.Add(injectScaleSign);
		}
	}

	[PunRPC]
	private void SyncColor(float r, float g, float b, float a)
	{
		if (this._spriteRenderer != null)
			this._spriteRenderer.color = new Color(r, g, b, a);
	}
}