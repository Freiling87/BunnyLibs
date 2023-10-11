using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;

namespace BunnyLibs
{
	[HarmonyPatch(typeof(Movement))]
	static class P_Movement_IModKnockback
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(typeof(Movement), nameof(Movement.KnockBack))]
		private static bool ApplyKnockbackBonus(Movement __instance, ref float strength, PlayfieldObject knockerObject)
		{
			logger.LogDebug($"=== ApplyKnockbackBonus: {knockerObject.name} knocking {__instance.name}");
			logger.LogDebug("\tStart: " + strength);

			if (__instance.isAgent)
			{
				Agent hitAgent = __instance.GetComponent<Agent>();
				float KnockbackResistance = hitAgent.GetOrAddHook<H_AgentStats>().StatKnockbackRes;
				strength *= KnockbackResistance;
				logger.LogDebug("Knockback Resistance: " + KnockbackResistance + ": " + strength);
			}

			if (knockerObject.isAgent) // Disable for ranged
			{
				Agent hitterAgent = knockerObject.GetComponent<Agent>();
				float KnockbackBonus = hitterAgent.GetOrAddHook<H_AgentStats>().StatMeleeKnockback;
				strength *= KnockbackBonus;
				logger.LogDebug("Knockback Bonus: " + KnockbackBonus + ": " + strength);
			}

			logger.LogDebug("--- Net Strength: " + strength);
			return true;
		}

		// ERROR: NRE when charging into border walls
		[HarmonyPostfix, HarmonyPatch(nameof(Movement.FindKnockBackStrength))]
		public static void FindKnockBackStrength(Movement __instance, PlayfieldObject ___playfieldObject, Agent ___agent, ref float __result)
		{
			if (___agent is null)
				return;

			Agent hitAgent = ___agent;
			Agent hitterAgent = ___playfieldObject.knockedByObject.playfieldObjectAgent;

			logger.LogDebug($"=== FindKnockbackStrength ({___agent.agentRealName}): {__result}");

			__result *= hitAgent.GetOrAddHook<H_AgentStats>().StatKnockbackRes;

			if (!(hitterAgent is null))
				__result *= hitterAgent.GetOrAddHook<H_AgentStats>().StatMeleeKnockback;

			logger.LogDebug("--- Net result: " + __result);
		}
	}
}
