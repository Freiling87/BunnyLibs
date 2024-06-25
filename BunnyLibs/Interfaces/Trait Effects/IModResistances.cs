using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	public interface IModResistances
	{
		// TODO: Invert any numerically since it was renamed resistance
		float ResistBullets { get; }
		float ResistExplosion { get; }
		float ResistFire { get; }
		float ResistKnockback { get; }
		float ResistMelee { get; } 
		float ResistPoison { get; }
	}

	[HarmonyPatch(typeof(Movement))]
	static class P_Movement_ResistKnockback
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		// ERROR: NRE when charging into border walls
		[HarmonyPostfix, HarmonyPatch(nameof(Movement.FindKnockBackStrength))]
		public static void ApplyKnockbackResistance(Agent ___agent, ref float __result)
		{
			if (___agent is null)
				return;

			__result /= ___agent.GetOrAddHook<H_AgentCachedStats>().ResistKnockback;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Movement), nameof(Movement.KnockBack))]
		private static bool ApplyKnockbackResistance2(Movement __instance, ref float strength)
		{
			//logger.LogDebug($"=== ApplyKnockbackResistance2: {__instance.name}");
			//logger.LogDebug("\tStart: " + strength);

			if (!__instance.isAgent)
				return true;
			
			Agent hitAgent = __instance.GetComponent<Agent>();
			float KnockbackResistance = hitAgent.GetOrAddHook<H_AgentCachedStats>().ResistKnockback;
			strength *= KnockbackResistance;
			//logger.LogDebug("Knockback Resistance: " + KnockbackResistance + ": " + strength);
			//logger.LogDebug("--- Net Strength: " + strength);
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_DamageReceived
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(nameof(PlayfieldObject.FindDamage), new[] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) })]
		private static IEnumerable<CodeInstruction> GetDamageRecvdMultiplier(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo statusEffects = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.statusEffects));
			MethodInfo applyDamageResistance = AccessTools.DeclaredMethod(typeof(P_PlayfieldObject_DamageReceived), nameof(P_PlayfieldObject_DamageReceived.ApplyDamageResistance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: true, // Nonstandard
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_1),		//	damagerObject
					new CodeInstruction(OpCodes.Ldloc_0),		//	agent
					new CodeInstruction(OpCodes.Ldloc_S, 7),	//	num [sic]	(Net Damage)
					new CodeInstruction(OpCodes.Call, applyDamageResistance),
					new CodeInstruction(OpCodes.Stloc_S, 7),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Ldfld, statusEffects),
					new CodeInstruction(OpCodes.Ldstr, VanillaEffects.NumbtoPain),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		private static float ApplyDamageResistance(PlayfieldObject damagerObject, Agent agent, float vanilla)
		{
			//logger.LogDebug($"=== GetDamageReceived: {agent.agentRealName} - VANILLA: {vanilla}");
			float resistance = 1f;

			if (damagerObject.isBullet)
			{
				// Works
			}
			else if (damagerObject.isMelee)
			{
				resistance *= agent.GetHook<H_AgentCachedStats>().ResistMelee;
			}
			else if (damagerObject.isObjectReal)
			{
				logger.LogDebug($"\tCaught isObjectReal");

			}
			else if (damagerObject.isFire)
			{
				logger.LogDebug($"\tCaught isFire");

			}

			//logger.LogDebug($"\tNET DAMAGE: {vanilla} / {resistance} = {vanilla / resistance}");
			return vanilla / resistance;
		}
	}
}