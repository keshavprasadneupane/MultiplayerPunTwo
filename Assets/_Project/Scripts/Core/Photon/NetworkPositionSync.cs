using Photon.Pun;
using UnityEngine;

public class NetworkPositionSync : MonoBehaviour, IPunObservable {
	// IPunObservable over RPC: RPCs are fire-and-forget events, not suited for
	// high-frequency state like position. IPunObservable hooks into Photon's
	// serialization loop, enabling per-packet interpolation and extrapolation.
	[SerializeField] Rigidbody2D _body;

	private Vector2 _networkPosition;
	private Vector2 _networkVelocity;
	private bool _hasData;

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
		if (stream.IsWriting) {
			stream.SendNext(this._body.position);
			stream.SendNext(this._body.linearVelocity);
		} else {
			var receivedPosition = (Vector2)stream.ReceiveNext();
			this._networkVelocity = (Vector2)stream.ReceiveNext();

			float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
			float clampedLag = Mathf.Min(lag, 1f / PhotonNetwork.SerializationRate);

			this._networkPosition = receivedPosition + this._networkVelocity * clampedLag;
			this._hasData = true;
		}
	}

	private void FixedUpdate() {
		if (!this._hasData) return;
		// why the extrapolation? because:
		// packet rate (~10/s) is far lower than FixedUpdate (~50/s), so dead-reckon
		// forward each tick using last known velocity, then lerp toward the
		// authoritative position to correct drift without visible snapping.


		// dead-reckon forward each tick so the body keeps moving smoothly
		// between packets (~10/s) rather than freezing at the last known position.
		this._networkPosition += this._networkVelocity * Time.fixedDeltaTime;

		// lerp speed scales with distance — large drift converges aggressively
		// (e.g. after a bounce), small drift corrects gently to avoid snapping.
		float lerpT = Mathf.Clamp01(
			Vector2.Distance(this._body.position, this._networkPosition)
			* Time.fixedDeltaTime * PhotonNetwork.SerializationRate
		);

		this._body.MovePosition(Vector2.Lerp(
			this._body.position,
			this._networkPosition,
			lerpT
		));
	}
}