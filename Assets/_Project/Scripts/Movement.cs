using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;


public interface IOrientable2D
{
	void SetCalculationScale(Vector2 localScaleSign);
}


public class Movement : LocalInitializationBase, IOrientable2D
{
	[SerializeField] Rigidbody2D _rigidbody2D;
	[SerializeField] float _speed = 5f;
	InputManager _inputManager;
	float directionX = 0f;
	private const float epsilon = 0.01f;
	private Vector2 localScaleSign = Vector2.one;
	private PhotonView _pv;
	private void Start()
	{
		Debug.Log($"Movement Start: Local Scale Sign determined as {this.localScaleSign}");
	}

	public void SetCalculationScale(Vector2 localScaleSign)
	{
		if (this._pv != null && this._pv.IsMine)
		{
			this.localScaleSign = localScaleSign;
			this._rigidbody2D.gravityScale = localScaleSign.y * Mathf.Abs(this._rigidbody2D.gravityScale);
		}
	}

	protected override bool OnLocalInit(PhotonView pv)
	{
		this._pv = pv != null ? pv : GetComponentInParent<PhotonView>();
		if (this._pv != null && !this._pv.IsMine)
		{
			this.enabled = false;
			if (this._rigidbody2D != null)
			{
				this._rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
				this._rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
			}
			return true;
		}

		this.enabled = true;
		this._inputManager = InputManager.Instance;
		this._inputManager.Subscribe(
			new InputActionSubscriptionLifetime<PlayerInputActionKey>(
				PlayerInputActionCollection.Player,
				PlayerInputActionKey.Move,
				this.Move,
				true
			)
		);
		return true;
	}


	private void FixedUpdate()
	{
		float currentY = this._rigidbody2D.linearVelocity.y;
		this._rigidbody2D.linearVelocity = new Vector2(this.directionX * this._speed, currentY);
	}
	public void Move(InputAction.CallbackContext context)
	{
		var direction = context.ReadValue<Vector2>();
		this.directionX = direction.x * this.localScaleSign.x;
	}


}
