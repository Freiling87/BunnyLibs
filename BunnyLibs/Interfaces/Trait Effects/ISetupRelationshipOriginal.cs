using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;

namespace BunnyLibs
{
	public interface ISetupRelationshipOriginal
	{
		/// <summary>
		/// Return null if this doesn't apply.
		/// </summary>
		public string GetRelationshipTo (Agent otherAgent);
		public bool IsRival(Agent otherAgent);
		/// <summary>
		/// Lower numbers will apply last, and therefore override higher ones. Use 1000 as a default.
		/// </summary> 
		public int Priority { get; }
	}

	public static class RelationshipHelper
	{
		public static void SetRelationshipTo(Agent agent, Agent otherAgent, string relationship, bool mutual = true)
		{
			Relationships relationships = agent.relationships;

			//	TODO: Verify these, since vanilla is totally inconsistent about it.
			switch (relationship)
			{
				case "":
					break;

				case VRelationship.Aligned:
					relationships.SetRel(otherAgent, VRelationship.Aligned);
					relationships.SetRelInitial(otherAgent, VRelationship.Aligned);
					relationships.SetRelHate(otherAgent, 0);
					relationships.SetSecretHate(otherAgent, false);
					break;

				case VRelationship.Annoyed:
					relationships.SetRel(otherAgent, VRelationship.Annoyed);
					relationships.SetRelInitial(otherAgent, VRelationship.Annoyed);
					relationships.SetStrikes(otherAgent, 2);
					break;

				case VRelationship.Friendly:
					relationships.SetRel(otherAgent, VRelationship.Friendly);
					relationships.SetRelInitial(otherAgent, VRelationship.Friendly);
					relationships.SetSecretHate(otherAgent, false);
					break;

				case VRelationship.Hostile:
					relationships.SetRel(otherAgent, VRelationship.Hostile);
					relationships.SetRelInitial(otherAgent, VRelationship.Hostile);
					relationships.SetRelHate(otherAgent, 5);
					relationships.GetRelationship(otherAgent).mechHate = true;
					break;

				case VRelationship.Loyal:
					relationships.SetRel(otherAgent, VRelationship.Loyal);
					relationships.SetRelInitial(otherAgent, VRelationship.Loyal);
					relationships.SetSecretHate(otherAgent, false);
					relationships.SetRelHate(otherAgent, 0);
					break;

				case VRelationship.Neutral:
					relationships.SetRel(otherAgent, VRelationship.Neutral);
					relationships.SetRelHate(otherAgent, 0);
					relationships.SetRelInitial(otherAgent, VRelationship.Neutral);
					relationships.SetSecretHate(otherAgent, false);
					break;

				case VRelationship.Submissive:
					relationships.SetRel(otherAgent, VRelationship.Submissive);
					relationships.SetRelInitial(otherAgent, VRelationship.Submissive);
					relationships.SetSecretHate(otherAgent, false);
					mutual = false;
					break;
			}

			if (mutual)
				SetRelationshipTo(otherAgent, agent, relationship, false);
		}
	}

	[HarmonyPatch(typeof(Relationships))]
	public static class P_Relationships_ISetupRelationshipOriginal
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(Relationships.SetupRelationshipOriginal))]
		private static void SetupRelationshipOriginal_Postfix(ref Agent ___agent, ref Agent otherAgent)
		{
			if (GC.levelType == VLevelType.HomeBase
				|| otherAgent.transforming
				|| ___agent.relationships.QuestInvolvement(otherAgent))
				return;

			string baseRelationship = ___agent.relationships.GetRel(otherAgent);

			foreach (ISetupRelationshipOriginal trait in ___agent.GetTraits<ISetupRelationshipOriginal>().OrderBy(t => t.Priority))
			{
				string newRelationship = trait.GetRelationshipTo(otherAgent);

				if (newRelationship is null)
					continue;

				logger.LogDebug($"- {trait}: ({___agent.agentRealName} / {otherAgent.agentRealName})");
				logger.LogDebug($"NewRel: {newRelationship}");
				logger.LogDebug($"  Assigning: {newRelationship}");
				RelationshipHelper.SetRelationshipTo(___agent, otherAgent, newRelationship, true);
			}
		}
	}

	[HarmonyPatch(typeof(StatusEffects))]
	public static class P_StatusEffects_ISetupRelationshipOriginal
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(StatusEffects.AgentIsRival))]
		private static void AgentIsRival_Postfix(Agent myAgent, StatusEffects __instance, ref bool __result)
		{
			Agent killedAgent = __instance.agent;

			foreach (ISetupRelationshipOriginal trait in myAgent.GetTraits<ISetupRelationshipOriginal>().OrderBy(t => t.Priority))
				__result = trait.IsRival(killedAgent);
		}
	}
}