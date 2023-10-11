using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BunnyLibs.Hooks
{
	internal class H_AgentHook
	{
		// Parent class with general setup/reset triggers
	}

	[HarmonyPatch(typeof(Agent))]
	public class P_Agent_AgentHookParent
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch("Start")]
		public static bool Start_CreateHook(Agent __instance)
		{
			//List<Type> agentHookTypes; // Assembly lookup?

			//foreach (Type type in agentHookTypes)
			//{
			//	// And then you gotta fuck with type here
			//	//__instance.GetOrAddHook<H_AgentStats>().Reset();

			//	__instance.GetOrAddHook<type>().Reset();
			//}

			return true;
		}
	}
}
