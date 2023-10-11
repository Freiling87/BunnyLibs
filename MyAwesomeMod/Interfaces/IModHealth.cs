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
	public interface IModHealthPerEndurance
	{
		int HealthPerEnduranceBonus { get; }
	}

	[HarmonyPatch(typeof(Agent))]
	static class P_Agent_Health
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(nameof(Agent.SetEndurance), new[] { typeof(int), typeof(bool) })]
		private static IEnumerable<CodeInstruction> SetHealthPerEndurance1(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_Agent_Health), nameof(P_Agent_Health.BaseHealthPerEndurance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_I4_S, 80),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, customMethod),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static int BaseHealthPerEndurance(Agent agent)
		{
			H_AgentStats hook = agent.GetHook<H_AgentStats>();
			return 4 * hook.StatHealthPerEndurance;
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(Agent.SetEndurance), new[] { typeof(int), typeof(bool) })]
		private static IEnumerable<CodeInstruction> SetHealthPerEndurance2(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_Agent_Health), nameof(P_Agent_Health.BonusHealthPerEndurance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_I4_S, 20),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, customMethod),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static int BonusHealthPerEndurance(Agent agent)
		{
			H_AgentStats hook = agent.GetHook<H_AgentStats>();
			return hook.StatHealthPerEndurance;
		}
	}
}
