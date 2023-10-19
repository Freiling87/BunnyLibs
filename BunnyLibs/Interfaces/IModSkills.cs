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
	public interface IModSkills
	{
		/// <summary>
		/// <code>
		/// string: Luck roll type. Use VLuckType or VLuckType_Skills
		/// int:    Flat luck bonus added to % rolls.
		/// </code>
		/// </summary>
		List<KeyValuePair<string, int>> SkillBonuses { get; }
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_IApplyLuckBonus
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(nameof(PlayfieldObject.DetermineLuck))]
		private static bool ApplyLuckBonus(PlayfieldObject __instance, ref int originalLuck, string luckType)
		{
			if (!__instance.isAgent)
				return true;

			Agent agent = __instance.playfieldObjectAgent;

			//	Due to hacky transpiler application.
			if (luckType == VLuckType_Skills.Dodge)
			{
				originalLuck = 0;

				if (agent.HasTrait(VanillaTraits.UnCrits))
					originalLuck = 5;
				else if (agent.HasTrait(VanillaTraits.UnCrits + "2"))
					originalLuck = 10;
			}

			foreach (IModSkills trait in __instance.playfieldObjectAgent.GetTraits<IModSkills>())
				foreach (KeyValuePair<string, int> keyValuePair in trait.SkillBonuses)
					if (keyValuePair.Key == luckType)
						originalLuck += keyValuePair.Value;

			return true;
		}
	}

	[HarmonyPatch(typeof(Relationships))]
	static class P_Relationships_IModLuck
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		// This and its opposite should be done in the same method.
		[HarmonyPostfix, HarmonyPatch(nameof(Relationships.FindThreat))]
		private static void ApplyLuckResistance(Agent ___agent, ref int __result)
		{
			foreach (IModSkills trait in ___agent.GetTraits<IModSkills>())
				foreach (KeyValuePair<string, int> keyValuePair in trait.SkillBonuses)
					if (keyValuePair.Key == VLuckType_Skills.Intimidation)
						__result -= keyValuePair.Value;
		}
	}

	[HarmonyPatch(typeof(StatusEffects))]
	public class P_StatusEffects_DodgeChance
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		//	Uses UnCrits+ gate. The else gate is ignored since this is always true.
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

		[HarmonyTranspiler, HarmonyPatch(nameof(StatusEffects.ChangeHealth), new[] { typeof(float), typeof(PlayfieldObject), typeof(NetworkInstanceId), typeof(float), typeof(string), typeof(byte) })]
		private static IEnumerable<CodeInstruction> ApplyDodgeEffects(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo agent = AccessTools.DeclaredField(typeof(StatusEffects), nameof(StatusEffects.agent));
			FieldInfo critChance = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.critChance));
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_StatusEffects_DodgeChance), nameof(P_StatusEffects_DodgeChance.CustomMethod));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_R4, 0f),
					new CodeInstruction(OpCodes.Starg_S, 1),
					new CodeInstruction(OpCodes.Br_S),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, customMethod),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static void CustomMethod(StatusEffects instance)
		{
			instance.CreateBuffText("Dodge", instance.agent.objectMult.IsFromClient());
		}
	}
}