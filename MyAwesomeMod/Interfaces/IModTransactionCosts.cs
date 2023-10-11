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

		static readonly List<string> IncludeEmployees = new List<string>()
		{
			VTransactionType.NPCMugging,
		};

		[HarmonyTranspiler, HarmonyPatch(nameof(PlayfieldObject.determineMoneyCost), new[] { typeof(int), typeof(string) })]
		private static IEnumerable<CodeInstruction> ApplyCostBonus(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo customMethod = AccessTools.DeclaredMethod(typeof(P_PlayfieldObject_CostScale), nameof(P_PlayfieldObject_CostScale.ApplySellerCostBonus));

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

		private static float ApplySellerCostBonus(PlayfieldObject instance, string transactionType, float num)
		{
			if (instance.playfieldObjectType != "Agent")
				return num;

			bool includeEmployees = IncludeEmployees.Contains(transactionType);
			Agent seller = instance.playfieldObjectAgent;
			Agent buyer = seller.interactingAgent;
			// Verified up to here
			List<Agent> buyerParty = new List<Agent>() { buyer };
			List<Agent> sellerParty = new List<Agent>() { seller };

			logger.LogDebug($"TT:{transactionType} Parties:{buyerParty.Count} / {sellerParty.Count}");

			if (includeEmployees)
			{
				buyerParty = GC.agentList.Where(a => a.employer == buyer || buyer.employer == a || a == buyer).ToList();
				sellerParty = GC.agentList.Where(a => a.employer == seller || seller.employer == a || a == seller).ToList();
			}

			foreach(Agent sellerAgent in sellerParty)
			{
				foreach (IModTransactionCosts trait in seller.GetTraits<IModTransactionCosts>())
				{
					logger.LogDebug("Trait: " + trait);

					foreach (KeyValuePair<string, float> kvp in trait.CostBonusesAsNPC)
					{
						if (kvp.Key == transactionType)
						{
							logger.LogDebug($"Applying Seller Cost Bonus {sellerAgent.agentRealName} / {trait}: {kvp.Value}, {transactionType}");
							num *= kvp.Value;
						}
					}
				}

			}

			foreach (Agent sellerAgent in buyerParty)
			{
				foreach (IModTransactionCosts trait in buyer.GetTraits<IModTransactionCosts>())
				{
					foreach (KeyValuePair<string, float> kvp in trait.CostBonusesAsPlayer)
					{
						if (kvp.Key == transactionType)
						{
							logger.LogDebug($"Applying Buyer Cost Bonus {sellerAgent.agentRealName} / {trait}: {kvp.Value}, {transactionType}");
							num /= kvp.Value;
						}
					}
				}
			}

			return num;
		}
	}
}