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
					new CodeInstruction(OpCodes.Ldloc_S, 7),	//	num [sic]
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
			logger.LogDebug($"=== GetDamageReceived: {agent.agentRealName} - VANILLA: {vanilla}");
			float damageMult = 1f;

			if (damagerObject.isBullet)
			{
				logger.LogDebug($"\tCaught Ranged");
			}
			else if (damagerObject.isMelee)
			{
				logger.LogDebug($"\tCaught Melee");
				H_AgentStats hook = agent.GetHook<H_AgentStats>();
				damageMult *= hook.StatMeleeRes;
				logger.LogDebug($"\t\tCachedMeleeDamageReceived: {hook.StatMeleeRes}");
				logger.LogDebug($"\t\tNet Damage Received: {damageMult}");
			}
			else if (damagerObject.isObjectReal)
			{
				logger.LogDebug($"\tCaught isObjectReal");

			}
			else if (damagerObject.isFire)
			{
				logger.LogDebug($"\tCaught isFire");

			}

			logger.LogDebug($"\tNET DAMAGE: {vanilla} * {damageMult} = {vanilla * damageMult}");
			return vanilla * damageMult;
		}
	}
}