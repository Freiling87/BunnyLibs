#		Bugs
##			Shapeshifter spawned as Average
Lost Diminutive
##			Bribe Cops
Doesn't add Above The Law

[Error  : Unity Log] ArgumentNullException: Value cannot be null.
Parameter name: agent
Stack trace:
RogueLibsCore.RogueExtensions.GetTraits[TTrait] (Agent agent) (at <a3d3f875b99344cba12c5f8ead40c647>:0)
BunnyLibs.P_PlayfieldObject_CostScale.ApplyCostEffects (PlayfieldObject instance, System.String transactionType, System.Single num) (at C:/Users/Owner/source/repos/SOR/BunnyLibs/BunnyLibs/Interfaces/IModTransactionCosts.cs:58)
PlayfieldObject.determineMoneyCost (System.Int32 moneyAmt, System.String transactionType) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
PlayfieldObject.determineMoneyCost (System.String transactionType) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
StatusEffectDisplay.RefreshStatusEffectText () (at <c91d003c54a541caabaa8c305d5e31e5>:0)
StatusEffects.RemoveStatusEffect (System.String statusEffectName, System.Boolean showText, UnityEngine.Networking.NetworkInstanceId cameFromClient, System.Boolean playSound) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
StatusEffects.RemoveStatusEffect (System.String statusEffectName, System.Boolean showText, System.Boolean playSound) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
AgentInteractions.PayCops (Agent agent, Agent interactingAgent) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
AgentInteractions.PressedButton (Agent agent, Agent interactingAgent, System.String buttonText, System.Int32 buttonPrice) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
RogueLibsCore.VanillaInteractions+<>c__DisplayClass10_2.<Patch_Agent>b__3 (RogueLibsCore.InteractionModel`1[T] m) (at <a3d3f875b99344cba12c5f8ead40c647>:0)
RogueLibsCore.SimpleInteraction`1[T].OnPressed () (at <a3d3f875b99344cba12c5f8ead40c647>:0)
RogueLibsCore.InteractionModel.OnPressedButton2 (System.String buttonName, System.Int32 buttonPrice) (at <a3d3f875b99344cba12c5f8ead40c647>:0)
RogueLibsCore.InteractionModel.OnPressedButton (System.String buttonName, System.Int32 buttonPrice) (at <a3d3f875b99344cba12c5f8ead40c647>:0)
RogueLibsCore.RogueLibsPlugin.PressedButtonHook2 (PlayfieldObject __instance, System.String buttonText, System.Int32 buttonPrice) (at <a3d3f875b99344cba12c5f8ead40c647>:0)
Agent.PressedButton (System.String buttonText, System.Int32 buttonPrice) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
WorldSpaceGUI.PressedButton (System.Int32 buttonNum) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
ButtonHelper.PushButton () (at <c91d003c54a541caabaa8c305d5e31e5>:0)
ButtonHelper.DoUpdate (System.Boolean onlySetImages) (at <c91d003c54a541caabaa8c305d5e31e5>:0)
ButtonHelper.Update () (at <c91d003c54a541caabaa8c305d5e31e5>:0)

#		Features

##			Stats
Be VERY sparing with cached stats - use only when necessary.
##			Interfaces
###				00 Interface Helper
###				IRefreshPerLevel				(Group)
####				IRefreshAtEndOfLevelStart
####				IRefreshAtLevelEnd
####				IRefreshPerLevel
###				IApplyDemographically
###				ICheckAgentLOS
###				IConditionallyAvailable
###				IModBodySize
####				Horizontal
When horizontal, the body dimensions are switched between X & Y.
###				IModHealth
###				IModifyItems
###				IModMeleeAttack
###				IModMovement
###				IModOperatingActions
###				IModResistances
###				IModSkills
####			C	Intimidation
Seems to REDUCE with more and better armed followers.
Also, a tiny character threatening a huge one has a ~25% chance...
####			T	Dodge A/V
Test
###				IModStatScariness
###				IModTransactionCosts
####			C	Clone Machine
Cost discount is reversed? Is the assignment of buyer/seller opposite with vending machines?
###				ISetupAgentStats
##			Names