
using UnityEngine;

namespace Kope.Core.Extensions
{


	public static class UnityTypeExtension
	{
		public static string GetFullHierarchyPath(this MonoBehaviour behaviour)
		{
			return $"(GameObjectPath: {behaviour.GetGameObjectHierarchyPath()})";
		}
		public static string GetGameObjectHierarchyPath(this MonoBehaviour behaviour)
		{
			System.Text.StringBuilder sb = new();
			Transform cursor = behaviour.gameObject.transform;

			while (cursor != null)
			{
				if (sb.Length > 0) sb.Insert(0, "->");
				sb.Insert(0, cursor.name);
				cursor = cursor.parent;
			}
			string sceneName = behaviour.gameObject.scene.name ?? "UnknownScene";
			return $"{sceneName}-->{sb}";
		}
	}
}
