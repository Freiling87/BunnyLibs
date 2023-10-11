using BepInEx;
using BepInEx.Logging;
using BTHarmonyUtils;
using HarmonyLib;
using RogueLibsCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BunnyLibs
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RogueLibs.GUID, RogueLibs.CompiledVersion)]
    public class Core : BaseUnityPlugin
    {
        public const string PluginGUID = "Freiling87.streetsofrogue.BunnyLibs";
        public const string PluginName = "BunnyLibs";
        public const string PluginVersion = "0.1.0";
		public const string subVersion = "a";

		public void Awake()
		{
			Harmony harmony = new Harmony(PluginGUID);
			harmony.PatchAll();
			PatcherUtils.PatchAll(harmony);
			RogueLibs.LoadFromAssembly();
			//CCULogoSprite = RogueLibs.CreateCustomSprite(nameof(Properties.Resources.CCU_160x160), SpriteScope.Interface, Properties.Resources.CCU_160x160).Sprite;
		}
	}

	public static class CoreTools
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		public static readonly System.Random random = new System.Random();

		public static T GetMethodWithoutOverrides<T>(this MethodInfo method, object callFrom)
				where T : Delegate
		{
			IntPtr ptr = method.MethodHandle.GetFunctionPointer();
			return (T)Activator.CreateInstance(typeof(T), callFrom, ptr);
		}

		public static string GetRandomMember(List<string> list) =>
			list[random.Next(0, list.Count - 1)];

		// SingletonsOfType might be better
		// T will take a base class
		public static List<T> AllClassesOfType<T>() where T : class
		{
			List<T> list = new List<T>();
			var derivedTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(T)));

			foreach (var type in derivedTypes)
			{
				var instance = Activator.CreateInstance(type) as T;
				list.Add(instance);
			}

			return list;
		}

		public static bool ContainsAll<T>(List<T> containingList, List<T> containedList) =>
			!containedList.Except(containingList).Any();

		// TODO: Works. This can be a lambda
		public static List<string> GetAllStringConstants(Type type)
		{
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			var stringConstants = new List<string>();

			foreach (var field in fields)
			{
				if (field.IsLiteral && field.FieldType == typeof(string))
				{
					string value = field.GetValue(null) as string;
					stringConstants.Add(value);
				}
			}

			return stringConstants;
		}
	}

	public static class BLLogger
	{
		private static string GetLoggerName(Type containingClass)
		{
			return $"CCU_{containingClass.Name}";
		}

		public static ManualLogSource GetLogger()
		{
			Type containingClass = new StackFrame(1, false).GetMethod().ReflectedType;
			return BepInEx.Logging.Logger.CreateLogSource(GetLoggerName(containingClass));
		}
	}

	public static class CoroutineExecutor
	{
		private class ExecutorBehaviour : MonoBehaviour { }

		private static GameObject executorGO;
		private static ExecutorBehaviour executorBehaviour;

		private static void EnsureExists()
		{
			if (executorGO != null)
			{
				return;
			}
			executorGO = new GameObject("CoroutineExecutorObject");
			executorBehaviour = executorGO.AddComponent<ExecutorBehaviour>();
			UnityEngine.Object.DontDestroyOnLoad(executorGO);
		}

		public static Coroutine StartCoroutine(IEnumerator routine)
		{
			EnsureExists();
			return executorBehaviour.StartCoroutine(routine);
		}
	}
}
