using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.Networking;

namespace BunnyLibs
{
	public interface IModLuck
	{
		List<KeyValuePair<string, int>> LuckBonuses { get; }
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_IApplyLuckBonus
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(nameof(PlayfieldObject.DetermineLuck))]
		private static bool ApplyLuckBonus(PlayfieldObject __instance, ref int originalLuck, string luckType)
		{
			logger.LogDebug("=== ApplyLuckBonus: " + originalLuck + " / " + luckType);

			if (!__instance.isAgent)
				return true;

			Agent agent = __instance.playfieldObjectAgent;

			// Re-apply vanilla effects counteracted elsewhere
			if (luckType == VLuckType.UnCrit)
			{
				if (agent.HasTrait(VanillaTraits.UnCrits))
					originalLuck = 5;
				else if (agent.HasTrait(VanillaTraits.UnCrits + "2"))
					originalLuck = 10;
				else originalLuck = 0;
			}

			foreach (IModLuck trait in __instance.playfieldObjectAgent.GetTraits<IModLuck>())
				foreach (KeyValuePair<string, int> keyValuePair in trait.LuckBonuses)
					if (keyValuePair.Key == luckType)
					{
						logger.LogDebug($"\tApplying Luck Bonus ({agent.agentRealName}):\n\tLuckType: {luckType}\n\tBonus: {keyValuePair.Value}");
						originalLuck += keyValuePair.Value;
					}


			return true;
		}


	}

	[HarmonyPatch(typeof(Relationships))]
	static class P_Relationships_LuckBonus
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(Relationships.FindThreat))]
		private static void ApplyLuckResistance(Agent ___agent, ref int __result)
		{
			logger.LogDebug($"\t\tLuckResistance: {___agent}");

			foreach (IModLuck trait in ___agent.GetTraits<IModLuck>())
				foreach (KeyValuePair<string, int> keyValuePair in trait.LuckBonuses)
					if (keyValuePair.Key == VLuckType.Intimidation)
					{
						logger.LogDebug($"\tApplying Luck Defense ({___agent.agentRealName}):\n\tLuckType: {VLuckType.Intimidation}\n\tBonus: {keyValuePair.Value}");
						__result -= keyValuePair.Value;
					}
		}
	}

	[HarmonyPatch(typeof(StatusEffects))]
	public class P_StatusEffects_DodgeChance
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(nameof(StatusEffects.ChangeHealth), new[] { typeof(float), typeof(PlayfieldObject), typeof(NetworkInstanceId), typeof(float), typeof(string), typeof(byte) })]
		private static IEnumerable<CodeInstruction> UnlockDodge(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo critChance = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.critChance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldstr, "ChanceAttacksDoZeroDamage2"),
					new CodeInstruction(OpCodes.Call),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Pop),		//	agent
					new CodeInstruction(OpCodes.Ldc_I4_1),	//	int 1 == true
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
	}
}