using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	public interface IModHealthPerEndurance
	{
		/// <summary>
		/// Number of additional HP gained per Endurance. Also applies to the base 80 health vanilla NPCs get, meaning four times this amount is added to base health.
		/// </summary>
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
			MethodInfo BaseHealthPerEndurance = AccessTools.DeclaredMethod(typeof(P_Agent_Health), nameof(P_Agent_Health.HealthPerEndurance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_I4_S, 80),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, BaseHealthPerEndurance),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(Agent.SetEndurance), new[] { typeof(int), typeof(bool) })]
		private static IEnumerable<CodeInstruction> SetHealthPerEndurance2(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo BaseHealthPerEndurance = AccessTools.DeclaredMethod(typeof(P_Agent_Health), nameof(P_Agent_Health.HealthPerEndurance));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_I4_S, 20),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, BaseHealthPerEndurance),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		private static int HealthPerEndurance(int vanilla, Agent agent)
		{
			//logger.LogDebug("HealthPerEndurance (Agent)");
			int newInt = vanilla; // Can't do ref, doesn't seem to work
			int timesToApply = (newInt / 20) + agent.enduranceStatMod;

			foreach (IModHealthPerEndurance trait in agent.GetTraits<IModHealthPerEndurance>())
			{
				newInt += trait.HealthPerEnduranceBonus * timesToApply;
				logger.LogDebug($"HPE: {trait} gives {trait.HealthPerEnduranceBonus}.");
				logger.LogDebug($"Added: {trait.HealthPerEnduranceBonus * timesToApply}");
			}

			return newInt;
		}
	}
}
