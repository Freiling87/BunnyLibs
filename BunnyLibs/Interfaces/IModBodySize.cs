using BepInEx.Logging;
using BTHarmonyUtils;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using JetBrains.Annotations;
using RogueLibsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BunnyLibs
{
	public interface IModBodySize
	{
		/// <summary>
		/// <code>
		/// Shrunk:            0.33f
		/// Diminutive:        0.66f
		/// Vanilla:           1.00f
		/// Giant:             3.00f
		/// </code>
		/// </summary>
		float HeightRatio { get; }
		/// <summary>
		/// <code>
		/// Shrunk:            0.33f
		/// Diminutive:        0.66f
		/// Vanilla:           1.00f
		/// Giant:             3.00f
		/// </code>
		/// </summary>
		float WidthRatio { get; }
	}

	public class BodySizeHelper : IRefreshAtEndOfLevelStart
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		public bool BypassUnlockChecks => true;

		public void Refresh()
		{
			// MUST use this method to catch BypassUnlockChecks
			foreach (Agent agent in GC.agentList)
				Refresh(agent);
		}
		public void Refresh(Agent agent)
		{
			Vector3 bodyScale = new(1.0f, 1.0f, agent.agentSpriteTransform.localScale.z);

			if (!agent.GetTraits<IModBodySize>().Any()
				|| agent.statusEffects.hasTrait(VanillaEffects.Shrunk)
				|| agent.statusEffects.hasTrait(VanillaEffects.Giant))
				return;

			foreach (IModBodySize trait in agent.GetTraits<IModBodySize>())
				bodyScale = new Vector3(bodyScale.x * trait.WidthRatio, bodyScale.y * trait.HeightRatio, bodyScale.z);

			agent.agentSpriteTransform.localScale = bodyScale;
			float bodySizeIndex = (bodyScale.x + bodyScale.y) / 2f;
			agent.agentCollider.radius = 0.24f + Mathf.Abs(1f - bodySizeIndex) * 0.12f;
			agent.wallDestroyDetector.circleCollider.radius = 0.96f * bodySizeIndex;
			Vector3 localPosition = agent.depthMask.transform.localPosition;
			agent.depthMask.transform.localPosition = new Vector3(localPosition.x, -0.82f, localPosition.z);
		}
		public bool RunThisLevel() => true;
	}
}