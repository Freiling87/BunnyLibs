using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System.Linq;

namespace BunnyLibs
{
	public interface IRefreshAtStartOfLevelStart : IRefreshPerLevel { }

	// TODO: Make this actually work
	[HarmonyPatch(typeof(LoadLevel))]
	public static class P_LoadLevel_RefreshAtStartOfLevelStart
	{
		public static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(typeof(LoadLevel), nameof(LoadLevel.NextLevel))]
		public static bool Refresh()
		{
			logger.LogDebug("=== RefreshAtStartOfLevelStart"); // Not triggering
			// Removed code: Add it back in from IRefreshAtEndOfLevelStart when it works
			//logger.LogDebug("=== RefreshAtStartOfLevelStart completed");
			return true;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(LoadLevel), nameof(LoadLevel.NextLevel))]
		private static void Test()
		{
			logger.LogDebug("LoadLevel.NextLevel postfix");
		}
	}
}