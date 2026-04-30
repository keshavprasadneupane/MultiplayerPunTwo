using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

// porting my InputManager Script from my previous project, with some adjustments to
// fit the new input system and project structure.

public enum PlayerInputActionCollection
{
	None = 0,
	Player = 1,
	UI = 11,
}


public enum PlayerInputActionKey
{
	None = 0,
	Move = 1,
	Fire = 11,
}

public readonly struct InputActionSubscriptionLifetime<TEnum> where TEnum : Enum
{
	public readonly PlayerInputActionCollection Map;
	public readonly TEnum Key;
	public readonly Action<InputAction.CallbackContext> Callback;
	public readonly bool IncludeCanceled;

	public InputActionSubscriptionLifetime(
		PlayerInputActionCollection map,
		TEnum key,
		Action<InputAction.CallbackContext> callback,
		bool includeCanceled = false
	)
	{
		this.Map = map;
		this.Key = key;
		this.Callback = callback;
		this.IncludeCanceled = includeCanceled;
	}
}

public class InputManager : MonoBehaviour
{
	private static InputManager instance;

	public static InputManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindFirstObjectByType<InputManager>();
				if (instance == null)
				{
					GameObject obj = new("InputManager");
					instance = obj.AddComponent<InputManager>();
				}
			}
			return instance;
		}
	}


	private CustomPlayerInput playerInput;
	private readonly Dictionary<PlayerInputActionCollection, InputActionMap> actionMaps = new();

	public CustomPlayerInput PlayerInputs => this.playerInput;

	public void Awake()
	{
		if (instance != null && instance != this)
		{
			Debug.LogWarning("Multiple instances of InputManager detected. Destroying duplicate.");
			Destroy(this.gameObject);
			return;
		}
		instance = this;
		DontDestroyOnLoad(this.gameObject);
		InitializeActionMaps();
	}

	private void InitializeActionMaps()
	{
		this.playerInput = new CustomPlayerInput();

		foreach (PlayerInputActionCollection type in Enum.GetValues(typeof(PlayerInputActionCollection)))
		{
			if (type == PlayerInputActionCollection.None) continue;

			InputActionMap map = GetActionMapByType(type);
			if (map != null)
			{
				actionMaps[type] = map;
			}
			else
			{
				Debug.LogWarning($"[InputManager] Failed to find physical ActionMap in Asset for Enum: {type}");
			}
		}

		DisableAllInputs();
		EnableActionType(PlayerInputActionCollection.Player);
	}

	private InputActionMap GetActionMapByType(PlayerInputActionCollection type)
	{
		return type switch
		{
			PlayerInputActionCollection.Player => this.playerInput.Player,
			PlayerInputActionCollection.UI => this.playerInput.UI,
			_ => null
		};
	}

	#region Map Management

	public void EnableActionType(PlayerInputActionCollection actionType)
	{
		if (actionMaps.TryGetValue(actionType, out var map)) map.Enable();
	}

	public void DisableActionType(PlayerInputActionCollection actionType)
	{
		if (actionMaps.TryGetValue(actionType, out var map)) map.Disable();
	}
	public void SwitchActionMap(PlayerInputActionCollection actionType)
	{
		DisableAllInputs();
		EnableActionType(actionType);
	}

	public void SetDefaultActionMap() => SwitchActionMap(PlayerInputActionCollection.Player);

	public void DisableAllInputs()
	{
		foreach (var kvp in actionMaps) kvp.Value.Disable();
	}

	#endregion

	#region Subscription System
	public void Subscribe<TEnum>(InputActionSubscriptionLifetime<TEnum> inputAction) where TEnum : Enum
	{
		if (actionMaps.TryGetValue(inputAction.Map, out var actionMap))
		{
			string actionName = inputAction.Key.ToString();
			var action = actionMap.FindAction(actionName);

			if (action != null)
			{
				action.performed += inputAction.Callback;
				if (inputAction.IncludeCanceled) action.canceled += inputAction.Callback;
			}
			else
			{
				Debug.LogWarning($"[InputManager] Action '{actionName}' not found in Map '{inputAction.Map}'. " +
								 "Ensure Enum name matches Action name in Input Asset exactly.");
			}
		}
	}

	public void SubscribeBulk<TEnum>(IEnumerable<InputActionSubscriptionLifetime<TEnum>> inputActions) where TEnum : Enum
	{
		foreach (var action in inputActions) Subscribe(action);
	}

	public void UnSubscribe<TEnum>(InputActionSubscriptionLifetime<TEnum> inputAction) where TEnum : Enum
	{
		if (actionMaps.TryGetValue(inputAction.Map, out var actionMap))
		{
			string actionName = inputAction.Key.ToString();
			var action = actionMap.FindAction(actionName);

			if (action != null)
			{
				action.performed -= inputAction.Callback;
				if (inputAction.IncludeCanceled) action.canceled -= inputAction.Callback;
			}
		}
	}

	public void UnSubscribeBulk<TEnum>(IEnumerable<InputActionSubscriptionLifetime<TEnum>> inputActions) where TEnum : Enum
	{
		foreach (var action in inputActions) UnSubscribe(action);
	}

	#endregion


	private void OnDestroy()
	{
		if (this.playerInput != null)
		{
			this.playerInput.Disable();
			this.playerInput.Dispose();
			this.playerInput = null;
		}
	}
}