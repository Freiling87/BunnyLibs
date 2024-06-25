using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System.Collections;

namespace BunnyLibs
{
	// TODO: When you switch to C# 8.0, use default implementation of some methods to reduce repetition across children of this interface.
	public interface IModifyItems
	{
		List<string> EligibleItemTypes { get; }
		List<string> ExcludedItems { get; }
		bool IsEligible(Agent agent, InvItem invItem); // Make this private when you upgrade
		void OnDrop(Agent agent, InvItem invItem);
		void OnPickup(Agent agent, InvItem invItem);
	}

	[HarmonyPatch(typeof(InvDatabase))]
	public static class P_InvDatabase 
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPriority(Priority.First)]
		[HarmonyPostfix, HarmonyPatch(nameof(InvDatabase.AddItemAtEmptySlot), new[] { typeof(InvItem), typeof(bool), typeof(bool), typeof(int), typeof(int) })]
		public static void AddItemAtEmptySlot_Postfix(InvDatabase __instance, ref InvItem item)
		{
			Agent agent = __instance.agent;

			if (agent is null || agent.agentName != VanillaAgents.CustomCharacter ||
				item is null || item.invItemName is null || item.invItemName == "")
				return;

			ModifyItemHelper.SetupItem(agent, item);
		}

		/// <summary>
		/// This SHOULD cover all non-Loadout item additions, like the 3 item slots in the editor.
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		[HarmonyPostfix, HarmonyPatch("AddItemReal")]
		public static void AddItemReal_InitSetup(InvDatabase __instance, ref InvItem __result)
		{
			if (__instance.agent is null)
				return;

			ModifyItemHelper.SetupItem(__instance.agent, __result);

			// Free NPC ammo
			if (__instance.agent.isPlayer == 0)
			{
				float ratio = (float)__result.maxAmmo / (float)__result.initCount;
				__result.invItemCount = (int)(__result.invItemCount * ratio);
			}
		}
	}

	[HarmonyPatch(typeof(InvItem))]
	public static class P_InvItem_IModifyItems
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(InvItem.SetupDetails))]
		private static void FinishSetupItem(InvItem __instance)
		{
			Agent agent = __instance.belongsToInventory?.agent ?? null;

			if (agent is null)
				return;

			ModifyItemHelper.SetupItem(agent, __instance);
		}
	}

	public static class ModifyItemHelper
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		public static void SetupInventory(Agent agent)
		{
			if (agent is null)
				return;

			agent.StartCoroutine(DelayedRecalc(agent));
		}
		// Waits one frame so that the newly-added trait is actually included in SetupInventory. OnAdded takes place before it is in the trait list.
		private static IEnumerator DelayedRecalc(Agent agent)
		{
			yield return null;

			foreach (InvItem invItem in agent.inventory.InvItemList.Where(ii => ii.invItemName != null && ii.invItemName != "").AddItem(agent.agentInvDatabase.fist))
				SetupItem(agent, invItem);
		}
		public static void SetupItem(Agent agent, InvItem invItem)
		{
			foreach (IModifyItems trait in agent.GetTraits<IModifyItems>())
				if (trait.IsEligible(agent, invItem))
					trait.OnPickup(agent, invItem);
		}
	}
}