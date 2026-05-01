using UnityEngine;

public class BallController : LocalInitializationBase, IOrientable2D {
	[SerializeField] private float initialSpeed = 5f;
	[SerializeField] private Vector2 initialDirection = new(0.1f, 1f);
	[SerializeField] private Rigidbody2D rb;
	private Vector2 _localScale = Vector2.one;
	public void SetCalculationScale(Vector2 localScaleSign) {
		if (!IsPhotonViewMine()) return;
		this._localScale = localScaleSign;
		this.rb.gravityScale = Mathf.Abs(this.rb.gravityScale) * localScaleSign.y;
	}

	protected override bool OnLocalInit() {
		if (this.rb == null) {
			Debug.LogError("Rigidbody2D component is not assigned in the inspector.");
			return false;
		}
		return true;
	}


	protected override void Start() {
		base.Start();
		if (!IsPhotonViewMine()) return;
		this.rb.linearVelocity = new Vector2(
			this._localScale.x * this.initialDirection.x * this.initialSpeed, 0f);
	}

	protected override void ViewNotMineLogic() {
		if (this.rb != null) {
			this.rb.bodyType = RigidbodyType2D.Kinematic;
			this.rb.interpolation = RigidbodyInterpolation2D.Interpolate;
			this.rb.linearVelocity = Vector2.zero;
		}
	}
}
