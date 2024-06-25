using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;

namespace BunnyLibs
{
	public interface ICopyTraitEffects
	{
		public List<string> TraitsToMimic { get; }
	}

	[HarmonyPatch(typeof(StatusEffects))]
	public static class P_StatusEffects_ICopyTraitEffects
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(typeof(StatusEffects), nameof(StatusEffects.hasTrait))]
		private static bool MimicTrait(StatusEffects __instance, string traitName, ref bool __result)
		{
			foreach (ICopyTraitEffects trait in __instance.agent.GetTraits<ICopyTraitEffects>())
			{
				if (trait.TraitsToMimic.Contains(traitName))
				{
					__result = true;
					return false;
				}
			}
			
			return true;
		}
	}
}