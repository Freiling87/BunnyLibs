using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System.Linq;

namespace BunnyLibs
{
	public interface ISetupAgentStats
	{
		/// <summary>
		/// Disaster/Mutator: Applies to all agents<br></br>
		/// Trait: Applies to agents who have the trait
		/// </summary>
		/// <param name="agent"></param>
		void SetupAgentStats(Agent agent);
	}

	[HarmonyPatch(declaringType: typeof(Agent))]
	public class P_Agent_ISetupAgentStats
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(Agent.SetupAgentStats))]
		public static void ApplySetupAgentStats(Agent __instance)
		{
			foreach (ISetupAgentStats trait in __instance.GetTraits<ISetupAgentStats>())
				trait.SetupAgentStats(__instance);

			foreach (ISetupAgentStats mutator in RogueFramework.Unlocks.OfType<MutatorUnlock>().OfType<ISetupAgentStats>().Where(u => ((MutatorUnlock)u).IsEnabled))
				foreach (Agent agent in GC.agentList)
					mutator.SetupAgentStats(agent);
		}
	}
}