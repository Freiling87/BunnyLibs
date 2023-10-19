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
	public interface IModStatScariness
	{
		float ScarinessAdded { get; }
	}

	[HarmonyPatch(typeof(Relationships))]
	public class P_Relationships_Scariness
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		// TODO: Vanilla only considers scary trait of one agent, I think.

		[HarmonyTranspiler, HarmonyPatch(nameof(Relationships.AssessFlee))]
		private static IEnumerable<CodeInstruction> ApplyScarinessBonus(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_Relationships_Scariness), nameof(P_Relationships_Scariness.GetScariness));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_1),				//	otherAgent
					new CodeInstruction(OpCodes.Ldloc_2),				//	num3
					new CodeInstruction(OpCodes.Call, customMethod),
					new CodeInstruction(OpCodes.Stloc_2),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_S),
					new CodeInstruction(OpCodes.Ldc_R4, 0f),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static float GetScariness(Agent agent, float scariness)
		{
			logger.LogDebug($"=== GetScariness ({agent.agentRealName}): {scariness}");
			foreach (IModStatScariness trait in agent.GetTraits<IModStatScariness>())
			{
				scariness += trait.ScarinessAdded;
			}
			
			foreach (IModStatScariness trait in agent.interactingAgent.GetTraits<IModStatScariness>())
			{
				scariness -= trait.ScarinessAdded;
			}

			logger.LogDebug($"    Net scariness for {agent.agentRealName}: {scariness}");
			return scariness;
		}
	}
}
