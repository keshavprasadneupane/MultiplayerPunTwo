using Kope.Core.Extensions;
using UnityEngine;

public abstract class NewworkInitializable : MonoBehaviour
{
	public bool IsInitialized { get; private set; } = false;
	private string sceneObjectPath;
	private bool messageLogged = false;

	public string LogMessage => this.sceneObjectPath;

	public void LocalInitialize()
	{
		Initialize(true);
	}

	public void RemoteInitialize()
	{
		Initialize(false);
	}

	private void Initialize(bool isLocal)
	{
		try
		{
			this.sceneObjectPath = this.GetFullHierarchyPath();
			if (this.IsInitialized) return;

			this.enabled = true;
			this.IsInitialized = isLocal ? OnLocalInit() : OnRemoteInit();
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"Error during initialization of {this.GetType().Name} at {this.sceneObjectPath}: {ex.Message}");
			this.enabled = false;
			this.IsInitialized = false;
		}
	}

	protected abstract bool OnLocalInit();

	protected virtual bool OnRemoteInit() => true;

	public void Update()
	{
		if (!this.IsInitialized)
		{
			if (!messageLogged)
			{
				if (string.IsNullOrEmpty(this.sceneObjectPath))
					this.sceneObjectPath = this.GetFullHierarchyPath();

				Debug.LogWarning($"Attempting to update {this.GetType().Name} before it is initialized. Path: {this.sceneObjectPath}");
				messageLogged = true;
			}
			return;
		}
		OnUpdate();
	}

	protected virtual void OnUpdate() { }
}