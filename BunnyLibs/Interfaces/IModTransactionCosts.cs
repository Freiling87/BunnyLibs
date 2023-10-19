using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Reflection;
using System.Reflection.Emit;

namespace BunnyLibs
{
	public interface IModTransactionCosts
	{
		List<KeyValuePair<string, float>> CostBonusesAsNPC { get; }
		List<KeyValuePair<string, float>> CostBonusesAsPlayer { get; }
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_CostScale
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(nameof(PlayfieldObject.determineMoneyCost), new[] { typeof(int), typeof(string) })]
		private static IEnumerable<CodeInstruction> ModifyMoneyCost(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_PlayfieldObject_CostScale), nameof(P_PlayfieldObject_CostScale.ApplyCostEffects));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction> 
				{ 
					new CodeInstruction(OpCodes.Ldarg_0),
				},
				targetInstructions: new List<CodeInstruction>
				{
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),	
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Call, customMethod),
					new CodeInstruction(OpCodes.Stloc_0),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldfld),
					new CodeInstruction(OpCodes.Ldstr, "Agent"),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		private static float ApplyCostEffects(PlayfieldObject instance, string transactionType, float num)
		{
			logger.LogDebug("ApplyCostEffects " + instance.name);

			Agent buyer = instance.interactingAgent;
			foreach (IModTransactionCosts trait in buyer.GetTraits<IModTransactionCosts>())
				foreach (KeyValuePair<string, float> kvp in trait.CostBonusesAsPlayer.Where(kvp => kvp.Key == transactionType && kvp.Value != 1f))
				{
					logger.LogDebug($"Applying Buyer Cost Bonus ({buyer.agentRealName} - {trait}): {num} / {kvp.Value} = {num / kvp.Value}");
					num /= kvp.Value;
				}

			if (instance.playfieldObjectType != "Agent")
				return num;

			Agent seller = instance.playfieldObjectAgent;
			foreach (IModTransactionCosts trait in seller.GetTraits<IModTransactionCosts>())
				foreach (KeyValuePair<string, float> kvp in trait.CostBonusesAsNPC.Where(kvp => kvp.Key == transactionType && kvp.Value != 1f))
				{
					logger.LogDebug($"Applying Seller Cost Bonus ({buyer.agentRealName} - {trait}): {num} * {kvp.Value} = {num * kvp.Value}");
					num *= kvp.Value;
				}

			return num;
		}
	}
}