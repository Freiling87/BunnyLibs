using BepInEx.Logging;
using BTHarmonyUtils;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using JetBrains.Annotations;
using RogueLibsCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	public interface IModOperatingActions
	{
		int NetToolCost(InvItem tool, int vanillaCost);
		float OperatingTime { get; }
		float OperatingVolume { get; }
	}

	[HarmonyPatch(typeof(Agent))]
	public class P_Agent_OperatingSpeed
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(nameof(Agent.FindOperatingTime))]
		private static bool ApplyOperatingSpeed(Agent __instance, ref float timeToUnlock)
		{
			foreach (IModOperatingActions trait in __instance.GetTraits<IModOperatingActions>())
				timeToUnlock *= trait.OperatingTime;

			logger.LogDebug($"--- Net Operating Speed ({__instance.agentRealName}): {timeToUnlock}");
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	public class P_PlayfieldObject_Operating_OperatingNoise
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTargetMethod, UsedImplicitly]
		private static MethodInfo Find_MoveNext_MethodInfo() =>
			PatcherUtils.FindIEnumeratorMoveNext(AccessTools.Method(typeof(PlayfieldObject), nameof(PlayfieldObject.Operating)));

		[HarmonyTranspiler, UsedImplicitly]
		private static IEnumerable<CodeInstruction> FilterInvestigationTextFromSpilledItems(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo ModOperatingVolume = AccessTools.DeclaredMethod(typeof(P_PlayfieldObject_Operating_OperatingNoise), nameof(P_PlayfieldObject_Operating_OperatingNoise.ModOperatingVolume));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				expectedMatches: 1,
				prefixInstructionSequence: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Ldfld),
					new CodeInstruction(OpCodes.Ldfld),
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Ldfld),
					new CodeInstruction(OpCodes.Callvirt),
					new CodeInstruction(OpCodes.Ldc_R4, 0.5f),
				},
				targetInstructionSequence: new List<CodeInstruction>
				{
				},
				insertInstructionSequence: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Call, ModOperatingVolume),
				},
				postfixInstructionSequence: new List<CodeInstruction>
				{
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		public static float ModOperatingVolume(Agent agent, float operatingVolume)
		{
			//	Vanilla = 0.5f
			//	Sneaky Fingers fully bypasses this

			logger.LogDebug($"Operating Volume ({agent.agentRealName}) : {operatingVolume}");

			foreach(IModOperatingActions trait in agent.GetTraits<IModOperatingActions>())
			{
				logger.LogDebug($"{trait} effect: {trait.OperatingVolume}");
				operatingVolume *= trait.OperatingVolume;
			}

			logger.LogDebug($"--- Net Operating noise: {operatingVolume}");
			return operatingVolume;
		}
	}
}