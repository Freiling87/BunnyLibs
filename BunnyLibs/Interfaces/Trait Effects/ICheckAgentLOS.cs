using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	public interface ICheckAgentLOS 
	{
		/// <summary><code>
		/// The number of LOS check cycles that must elapse for the LOS behavior to occur.
		/// Cannibal, Slum Dweller:          8
		/// Thief, Vampire:                  9
		/// </code></summary>
		int LOSInterval { get; }

		/// <summary><code>
		/// The range in tiles between the agent and its target object.
		/// Thief:                           4.00
		/// Vampire:                         5.00
		/// Cannibal, Slum Dweller:          5.64
		/// </code></summary>
		float LOSRange { get; }

		/// <summary>
		/// Note that agent.losCheckAtIntervalsTime must be manually reset to 0, if you want that to happen.
		/// </summary>
		void LOSAction();
	}

	//	Current version of C# doesn't support default interface implementation.
	internal class AgentLOSHelper : ISetupAgentStats
	{
		//	ISetupAgentStats
		public bool BypassUnlockChecks => true;
		public void SetupAgent(Agent agent)
		{
			if (agent.HasTrait<ICheckAgentLOS>())
				agent.losCheckAtIntervals = true;
		}
	}

	[HarmonyPatch(typeof(BrainUpdate))]
	public static class P_BrainUpdate_CheckAgentLOS
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(typeof(BrainUpdate), nameof(BrainUpdate.MyUpdate))]
		private static IEnumerable<CodeInstruction> CallCustomLOSChecks(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo runChecks = AccessTools.DeclaredMethod(typeof(P_BrainUpdate_CheckAgentLOS), nameof(P_BrainUpdate_CheckAgentLOS.RunChecks));
			FieldInfo losCheckAtIntervalsTime = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.losCheckAtIntervalsTime));
			FieldInfo agent = AccessTools.DeclaredField(typeof(BrainUpdate), "agent");

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Call, runChecks),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld),
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Ldfld, losCheckAtIntervalsTime),
					new CodeInstruction(OpCodes.Ldc_I4_1),
					new CodeInstruction(OpCodes.Add),
					new CodeInstruction(OpCodes.Stfld),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		private static void RunChecks(Agent agent)
		{
			foreach(ICheckAgentLOS trait in agent.GetTraits<ICheckAgentLOS>().Where(t => agent.losCheckAtIntervalsTime > t.LOSInterval))
			{
				trait.LOSAction();
			}
		}
	}
}