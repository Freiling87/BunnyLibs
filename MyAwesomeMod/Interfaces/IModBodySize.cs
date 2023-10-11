using BepInEx.Logging;
using RogueLibsCore;
using UnityEngine;

namespace BunnyLibs
{
	public interface IModBodySize
	{
		float HeightRatio { get; }
		float WidthRatio { get; }
	}

	public static class AgentBodySizeHelper
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		public static void ApplySize(Agent agent)
		{
			Vector3 bodyScale = Vector3.one;
			float z = agent.agentSpriteTransform.localScale.z;

			if (agent.statusEffects.hasTrait(VanillaEffects.Shrunk))
			{
				//bodyScale = new Vector3(0.333333f, 0.333333f, 1.0f);
				//agent.StartCoroutine(agent.statusEffects.BecomeShrunk());

				// Test with simple return first
				return;
			}
			else if (agent.statusEffects.hasTrait(VanillaEffects.Giant))
			{
				// Test with simple return first
				return;
			}

			foreach (IModBodySize trait in agent.GetTraits<IModBodySize>())
			{
				logger.LogDebug($"{trait} Bodyscale: {trait.WidthRatio} / {trait.HeightRatio}");
				bodyScale *= new Vector2(trait.WidthRatio, trait.HeightRatio);
			}

			logger.LogDebug($"Net Bodyscale: {bodyScale.x} / {bodyScale.y}");

			agent.agentSpriteTransform.localScale = bodyScale;
			float averagedColliderSize = (bodyScale.x + bodyScale.y) / 2f;
			agent.agentCollider.radius = 0.24f + Mathf.Abs(1f - averagedColliderSize) * 0.12f; // Testing hitbox adjustment
		}
	}
}