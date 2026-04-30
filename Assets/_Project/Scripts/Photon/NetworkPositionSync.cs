using Photon.Pun;
using UnityEngine;

public class NetworkPositionSync : MonoBehaviour, IPunObservable
{
	// IPunObservable over RPC: RPCs are fire-and-forget events, not suited for
	// high-frequency state like position. IPunObservable hooks into Photon's
	// serialization loop, enabling per-packet interpolation and extrapolation.
	[SerializeField] Rigidbody2D _body;
	private Vector2 _networkPosition;
	private float _distance;
	private bool _hasData;

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(this._body.position);
			stream.SendNext(this._body.linearVelocity);
		}
		else
		{
			var receivedPosition = (Vector2)stream.ReceiveNext();
			var receivedVelocity = (Vector2)stream.ReceiveNext();

			float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
			// only extrapolate up to the next expected update to prevent 
			// overshooting due to network hiccups or low serialization rates
			float clampedLag = Mathf.Min(lag, 1f / PhotonNetwork.SerializationRate);

			this._networkPosition = receivedPosition + receivedVelocity * clampedLag;
			this._distance = Vector2.Distance(this._body.position, this._networkPosition);
			this._hasData = true;
		}
	}


	private void FixedUpdate()
	{
		if (!_hasData) return;
		this._body.MovePosition(Vector2.MoveTowards(
			this._body.position,
			this._networkPosition,
			this._distance * (1f / PhotonNetwork.SerializationRate)
		));
	}
}