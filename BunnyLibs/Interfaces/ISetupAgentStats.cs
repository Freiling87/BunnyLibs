using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System;
using System.Linq;
using System.Reflection;

namespace BunnyLibs
{
	/// <summary>
	/// Order of application: Trait, StatusEffect, Mutator, Disaster, Other
	///		where Other is a system that uses BypassUnlockChecks == true to apply Refresh()
	/// </summary>
	public interface ISetupAgentStats
	{
		/// <summary><code>
		/// Disaster, Mutator:                              Apply to all agents
		/// StatusEffect, Trait:                            Apply to agents with the trait/SE
		/// </code></summary>
		/// <param name="agent"></param>
		void SetupAgent(Agent agent);

		/// <summary>
		/// Set to TRUE in order to apply SetupAgent regardless of whether any unlocks are active.
		/// </summary>
		bool BypassUnlockChecks { get; }
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
				trait.SetupAgent(__instance);

			foreach (ISetupAgentStats statusEffect in __instance.GetEffects<ISetupAgentStats>())
				statusEffect.SetupAgent(__instance);

			foreach (ISetupAgentStats mutator in RogueFramework.Unlocks.OfType<MutatorUnlock>().OfType<ISetupAgentStats>().Where(u => ((MutatorUnlock)u).IsEnabled))
				mutator.SetupAgent(__instance);

			if (RogueFramework.GetActiveDisaster() is ISetupAgentStats disaster)
				disaster.SetupAgent(__instance);

			// All this to get a bool that oughtta be static.
			// TODO: Cache Singleton instances
			foreach (Type type in InterfaceHelper.TypesImplementingInterface(typeof(ISetupAgentStats)))
			{
				if (type == typeof(ISetupAgentStats) || type.IsAbstract)	//	Only parent interface
					continue;

				PropertyInfo propertyInfo = type.GetProperty(nameof(ISetupAgentStats.BypassUnlockChecks));

				if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
				{
					object instance = Activator.CreateInstance(type);
					bool value = (bool)propertyInfo.GetValue(instance, null);

					if (value)
						((ISetupAgentStats)instance).SetupAgent(__instance);
				}
			}
		}
	}
}