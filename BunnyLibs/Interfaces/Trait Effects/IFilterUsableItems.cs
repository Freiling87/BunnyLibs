using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;

namespace BunnyLibs.Interfaces
{
	public interface IFilterUsableItems
	{
		public abstract List<string> ItemNames { get; }
		/// <summary>
		/// If left null, uses a generic dialogue.
		/// </summary>
		public abstract List<string>? RefusalDialogueNames { get; }
		/// <summary>
		/// Default = 20% per attempt
		/// </summary>
		public abstract int? DialogueChance { get; }
	}

	[HarmonyPatch(typeof(ItemFunctions))]
	internal static class P_ItemFunctions_IFilterUsableItems
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		private static readonly int BaseDialogueChance = 20;
		public const string GenericItemRefusal = "GenericItemRefusal";

		[RLSetup]
		private static void Setup()
		{
			RogueLibs.CreateCustomName(GenericItemRefusal, NameTypes.Dialogue, new CustomNameInfo
			{
				[LanguageCode.English] = "I can't use this.",
			});
		}

		[HarmonyPrefix, HarmonyPatch(nameof(ItemFunctions.UseItem), new Type[] { typeof(InvItem), typeof(Agent) })]
		private static bool FilterItemUse(InvItem item, Agent agent)
		{
			logger.LogDebug($"FilterItemUse: {item.invItemName} / {agent.agentRealName}");
			foreach(IFilterUsableItems trait in agent.GetTraits<IFilterUsableItems>())
			{
				logger.LogDebug($"\ttrait: {trait}");
				logger.LogDebug($"\ttrait: {trait.ItemNames.Count}");
				if (!(trait.ItemNames is null) && trait.ItemNames.Contains(item.invItemName))
				{
					logger.LogDebug($"\t\tDialogue");
					int threshold = trait.DialogueChance ?? BaseDialogueChance; 

					if (UnityEngine.Random.Range(1, 100) <= threshold)
					{
						string dialogueName = nameof(GenericItemRefusal);

						if (!(trait.RefusalDialogueNames is null) && trait.RefusalDialogueNames.Count > 0)
						{
							int roll = UnityEngine.Random.Range(0, trait.RefusalDialogueNames.Count - 1);
							dialogueName = trait.RefusalDialogueNames[roll];
						}

						agent.SayDialogue(dialogueName);
					}

					GC.audioHandler.Play(agent, VanillaAudio.CantDo);

					return false;
				}
			}

			return true;
		}
	}
}
