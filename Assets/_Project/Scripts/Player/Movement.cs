using UnityEngine;
using UnityEngine.InputSystem;

public interface IOrientable2D {
	void SetCalculationScale(Vector2 localScaleSign);
}

public class Movement : LocalInitializationBase, IOrientable2D {
	[SerializeField] Rigidbody2D _rigidbody2D;
	[SerializeField] float _speed = 5f;
	InputManager _inputManager;
	float _directionX;
	Vector2 _localScaleSign = Vector2.one;

	public void SetCalculationScale(Vector2 localScaleSign) {
		if (!IsPhotonViewMine()) return;
		this._localScaleSign = localScaleSign;
		this._rigidbody2D.gravityScale = localScaleSign.y * Mathf.Abs(this._rigidbody2D.gravityScale);
	}

	protected override bool OnLocalInit() {
		this._inputManager = InputManager.Instance;
		this._inputManager.Subscribe(
			new InputActionSubscriptionLifetime<PlayerInputActionKey>(
				PlayerInputActionCollection.Player,
				PlayerInputActionKey.Move,
				Move,
				true
			)
		);
		return true;
	}

	protected override void ViewNotMineLogic() {
		if (this._rigidbody2D == null) return;
		this._rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
		this._rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
	}

	private void FixedUpdate() {
		float currentY = this._rigidbody2D.linearVelocity.y;
		this._rigidbody2D.linearVelocity = new Vector2(this._directionX * this._speed, currentY);
	}

	public void Move(InputAction.CallbackContext context) {
		this._directionX = context.ReadValue<Vector2>().x * this._localScaleSign.x;
	}
}