using UnityEngine;
using Kope.Core.Extensions;

[System.Serializable]
public struct InitializationLogger {
	private string _sceneObjectPath;
	private bool _warningLogged;

	public readonly string ScenePath => _sceneObjectPath;

	public void CachePath(MonoBehaviour context) {
		if (string.IsNullOrEmpty(this._sceneObjectPath)) {
			this._sceneObjectPath = context.GetFullHierarchyPath();
		}
	}

	public void LogInitializationError(MonoBehaviour context, System.Exception ex) {
		CachePath(context);
		Debug.LogError($"[Init Error] {context.GetType().Name} at {this._sceneObjectPath}: {ex.Message}");
	}

	public void LogUninitializedUpdate(MonoBehaviour context) {
		if (this._warningLogged) return;

		CachePath(context);
		Debug.LogWarning($"[Sequence Warning] Attempting to update {context.GetType().Name} before initialization. Path: {this._sceneObjectPath}");
		this._warningLogged = true;
	}
}