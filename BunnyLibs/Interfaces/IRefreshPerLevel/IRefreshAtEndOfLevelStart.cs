using BepInEx.Logging;
using BTHarmonyUtils;
using BTHarmonyUtils.TranspilerUtils;
using BunnyLibs.ParentInterfaces;
using HarmonyLib;
using JetBrains.Annotations;
using RogueLibsCore;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	/// <summary><code>
	/// Order of application: Mutator, Disaster, Trait, StatusEffect, Other
	///		where Other means a system that uses BypassUnlockChecks == True to trigger Refresh().
	/// </code></summary>
	public interface IRefreshAtEndOfLevelStart : IRefreshPerLevel { }

	[HarmonyPatch(typeof(LoadLevel))]
	public static class P_LoadLevel_IRefreshAtEndOfLevelStart
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyTargetMethod, UsedImplicitly]
		private static MethodInfo Find_MoveNext_MethodInfo() =>
			PatcherUtils.FindIEnumeratorMoveNext(AccessTools.Method(typeof(LoadLevel), "SetupMore4_2"));

		[HarmonyTranspiler, UsedImplicitly]
		private static IEnumerable<CodeInstruction> RefreshAtEndOfLevelStart(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo refreshAgents = AccessTools.DeclaredMethod(typeof(P_LoadLevel_IRefreshAtEndOfLevelStart), nameof(P_LoadLevel_IRefreshAtEndOfLevelStart.Refresh));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldstr, "SETUPMORE4_7"),
					new CodeInstruction(OpCodes.Call),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Call, refreshAgents),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		public static void Refresh()
		{
			foreach (IRefreshAtEndOfLevelStart mutator in RogueFramework.Unlocks.OfType<MutatorUnlock>().Where(m => m.IsEnabled).OfType<IRefreshAtEndOfLevelStart>())
				mutator.Refresh();

			foreach (IRefreshAtEndOfLevelStart disaster in RogueFramework.CustomDisasters.Where(cd => cd.IsActive).OfType<IRefreshAtEndOfLevelStart>().Where(d => d.RunThisLevel()))
				disaster.Refresh();

			foreach (Agent agent in GC.agentList)
			{
				foreach (IRefreshAtEndOfLevelStart trait in agent.GetTraits<IRefreshAtEndOfLevelStart>().Where(t => t.RunThisLevel()))
					trait.Refresh(agent);

				foreach(IRefreshAtEndOfLevelStart statusEffect in agent.GetEffects<IRefreshAtEndOfLevelStart>().Where(se => se.RunThisLevel()))
					statusEffect.Refresh(agent);
			}

			// TODO: Cache Singleton instances
			foreach (Type type in InterfaceHelper.TypesImplementingInterface(typeof(IRefreshAtEndOfLevelStart)))
			{
				if (type.IsAbstract || type.IsInterface)
					continue;

				PropertyInfo propertyInfo = type.GetProperty(nameof(IRefreshAtEndOfLevelStart.BypassUnlockChecks));

				if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
				{
					object instance = Activator.CreateInstance(type);
					bool value = (bool)propertyInfo.GetValue(instance, null);

					if (value)
						((IRefreshAtEndOfLevelStart)instance).Refresh();
				}
			}
		}
	}
}