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
	public interface IModMoveSpeed
	{
		float Acceleration { get; }
		float MoveSpeedMax { get; }
	}

	[HarmonyPatch(typeof(Agent))]
	static class P_Agent_Acceleration
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		static readonly float accelerationHardcode = 0.1f;

		[HarmonyTranspiler, HarmonyPatch(nameof(Agent.GetCurSpeed), new[] { typeof(bool) })]
		private static IEnumerable<CodeInstruction> SetAcceleration(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo getMoveSpeed = AccessTools.DeclaredMethod(typeof(P_Agent_Acceleration), nameof(P_Agent_Acceleration.GetAcceleration));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
				new CodeInstruction(OpCodes.Ldc_R4, accelerationHardcode),
				},
				insertInstructions: new List<CodeInstruction>
				{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Call, getMoveSpeed),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		private static float GetAcceleration(Agent agent)
		{
			logger.LogDebug("GetAcceleration");
			H_AgentStats hook = agent.GetHook<H_AgentStats>();
			logger.LogDebug($"=== CurSpeed for  {agent.agentRealName}");
			logger.LogDebug($"    Acceleration:   {hook.StatAcceleration}");
			return (int)(accelerationHardcode * hook.StatAcceleration);
		}
	}

	[HarmonyPatch(typeof(Agent))]
	static class P_Agent_MaxSpeed
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(Agent.FindSpeed))]
		private static void LogMMoveSpeedMax(Agent __instance, ref int __result)
		{
			logger.LogDebug($"=== LogMoveSpeedMax: {__instance.agentRealName}.speedMax = {__instance.speedMax} ");
		}
		[HarmonyTranspiler, HarmonyPatch(nameof(Agent.FindSpeed))]
		private static IEnumerable<CodeInstruction> SetMoveSpeedMax(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo getMoveSpeed = AccessTools.DeclaredMethod(typeof(P_Agent_MaxSpeed), nameof(P_Agent_MaxSpeed.GetMoveSpeedMax));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_I4, 250),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, getMoveSpeed),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static int GetMoveSpeedMax(Agent agent)
		{
			H_AgentStats hook = agent.GetHook<H_AgentStats>();
			return (int)(250f * hook.StatMaxSpeed);
		}
	}
}