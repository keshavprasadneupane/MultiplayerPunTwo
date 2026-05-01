using System;
using UnityEngine;

public class BallDirectionCollider : MonoBehaviour {
	[SerializeField] private Collider2D col2d;
	[SerializeField] private LayerMask layerMask;
	public Action OnColliderEnter;

	public void Start() {
		this.col2d.isTrigger = true;
	}
	public void OnTriggerEnter2D(Collider2D collision) {
		if ((this.layerMask.value & (1 << collision.gameObject.layer)) == 0) {
			return;
		}
		this.OnColliderEnter?.Invoke();
	}
}
