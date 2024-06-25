using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace BunnyLibs
{
	public interface IUpgradeVanillaTrait
	{
		public List<string> BaseTraits { get; }
	}

	[HarmonyPatch(typeof(Unlocks))]
	internal static class P_Unlocks_Debug
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(typeof(Unlocks), nameof(Unlocks.LoadInitialUnlocks))]
		internal static void SetTraitUpgrades()
		{
			foreach (Type upgradeTrait in InterfaceHelper.TypesImplementingInterface(typeof(IUpgradeVanillaTrait)))
			{
				if (upgradeTrait.IsAbstract || upgradeTrait.IsInterface)
					continue;

				object traitInstance = Activator.CreateInstance(upgradeTrait);
				PropertyInfo baseTraitsField = upgradeTrait.GetProperty(nameof(IUpgradeVanillaTrait.BaseTraits));
				List<string> baseTraits = (List<string>)baseTraitsField.GetValue(traitInstance, null);

				foreach (Unlock baseTrait in GC.sessionDataBig.unlocks.Where(u => baseTraits.Contains(u.unlockName)))
					baseTrait.upgrade = upgradeTrait.Name;
			}
		}
	}
}