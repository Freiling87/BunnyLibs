using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BunnyLibs
{
	public class H_AgentCachedStats : HookBase<PlayfieldObject>
	{
		// NOTE: Instance = host Agent

		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		Agent agent => (Agent)Instance;

		protected override void Initialize() { }
		public void Reset() { }

		//	Note that all of the below are just the bonuses and do not necessarily reflect the full "stat value."
		//	E.g., Melee Damage could show as 100% here, while the agent has other damage-boosting traits that are taken care of by vanilla code.
		//	So if this gets to the point of a user Stat Sheet interface, the display totals will need to be slightly tweaked to accurately reflect the stats' values.

		// TODO
		public void LogStatSheet() { }


		//	IModMeleeAttack

		/// <summary><code>
		///		Weak                    0.50f
		///		Withdrawal              0.75f
		///		Strength (Small)        1.25f
		///		Strength                1.50f
		/// </code></summary>
		public float MeleeDmg =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeDmg)}", CalcMeleeDmg, 1);
		private float CalcMeleeDmg()
		{
			float damageMultiplier = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyModMeleeAttack() && t.MeleeDamage != 1f))
				damageMultiplier *= trait.MeleeDamage;

			if (agent.HasEffect(VStatusEffect.Giant)
				|| agent.HasEffect(VStatusEffect.Shrunk)
				|| agent.HasTrait(VanillaTraits.Diminutive))
				damageMultiplier *= agent.agentSpriteTransform.localScale.x;

			return damageMultiplier;
		}

		/// <summary><code>
		/// 
		/// </code></summary>
		public float MeleeKnockback =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeKnockback)}", CalcMeleeKnockback, 1);
		private float CalcMeleeKnockback()
		{
			float strength = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyModMeleeAttack() && t.MeleeKnockback != 1f))
				strength *= trait.MeleeKnockback;

			return strength;
		}

		/// <summary><code>
		/// Vanilla multipliers:
		///		Paralyzed               0.00f
		///		Melee Mobility          0.50f
		///		Long Lunge              1.50f
		///		Long Lunge +            1.80f
		/// </code></summary>
		public float MeleeLunge =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeLunge)}", CalcMeleeLunge, 1);
		private float CalcMeleeLunge()
		{
			float lunge = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyModMeleeAttack() && t.MeleeLunge != 1f))
			{
				lunge *= trait.MeleeLunge;
			}

			return lunge;
		}

		/// <summary><code>
		/// Fist, Knife, Baton, Wrench  5.00f
		/// Sledge, Crowbar, Axe, Bat   4.00f
		/// </code></summary>
		public float MeleeSpeed =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeSpeed)}", CalcMeleeSpeed, 1);
		private float CalcMeleeSpeed()
		{
			float speed = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyModMeleeAttack() && t.MeleeSpeed != 1f))
			{
				speed *= trait.MeleeSpeed;
			}

			return speed;
		}


		//	IModMoveSpeed

		/// <summary>
		/// CURRENTLY NOT IMPLEMENTED
		/// </summary>
		public float SpeedAcceleration =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcAcceleration)}", CalcAcceleration, 1);
		private float CalcAcceleration()
		{
			float accelerationMult = 1f;

			foreach (IModMovement trait in agent.GetTraits<IModMovement>().Where(t => t.Acceleration != 1f))
			{
				//logger.LogDebug($"Applying bonus ({trait}): {trait.Acceleration}");
				accelerationMult *= trait.Acceleration;
			}

			//logger.LogDebug($"Net Accel Bonus: {accelerationMult}");

			return accelerationMult;
		}

		/// <summary>
		/// 
		/// </summary>
		public float SpeedMax =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMaxSpeed)}", CalcMaxSpeed, 1);
		private float CalcMaxSpeed()
		{
			float maxSpeedMult = 1f;

			foreach (IModMovement trait in agent.GetTraits<IModMovement>().Where(t => t.MoveSpeedMax != 1f))
			{
				//logger.LogDebug($"Applying bonus ({trait}): {trait.MoveSpeedMax}");
				maxSpeedMult *= trait.MoveSpeedMax;
			}

			//logger.LogDebug($"Net Max Speed Bonus: {maxSpeedMult}");

			return maxSpeedMult;
		}


		//	IModResistances

		/// <summary>
		/// 
		/// </summary>
		public float ResistMelee =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeRes)}", CalcMeleeRes, 1);
		private float CalcMeleeRes()
		{
			// Tested, works
			float meleeRes = 1f;

			foreach (IModResistances trait in agent.GetTraits<IModResistances>().Where(t => t.ResistMelee != 1f))
			{
				meleeRes *= trait.ResistMelee;
			}

			return meleeRes;
		}

		/// <summary>
		/// 
		/// </summary>
		public float ResistKnockback =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcKnockbackRes)}", CalcKnockbackRes, 1);
		private float CalcKnockbackRes()
		{
			float knockback = 1f;

			foreach (IModResistances trait in agent.GetTraits<IModResistances>().Where(t => t.ResistKnockback != 1f))
				knockback *= trait.ResistKnockback;

			return knockback;
		}


		//	Cache Mgr.
		private static Dictionary<string, Tuple<float, float>> statCache = new Dictionary<string, Tuple<float, float>>();
		private static readonly int cacheTTLSeconds = 1;

		public static float GetCachedStat(string cacheUID, Func<float> calculateStat, int maxAge = 1)
		{
			lock (statCache)
			{
				if (statCache.TryGetValue(cacheUID, out var cacheEntry) && ((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - cacheEntry.Item2) <= maxAge)
				{
					//logger.LogDebug($"GetCachedStat         : {cacheUID}: {cacheEntry.Item1}");
					return cacheEntry.Item1;
				}

				float statValue = calculateStat();
				//logger.LogDebug($"GetCachedStat [RECALC]: {cacheUID}: {statValue}");
				statCache[cacheUID] = Tuple.Create(statValue, (float)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
				return statValue;
			}
		}
	}

	[HarmonyPatch(typeof(Agent))]
	internal class P_Agent_AgentStatsHook
	{
		[HarmonyPrefix, HarmonyPatch("Start")]
		public static bool Start_CreateHook(Agent __instance)
		{
			__instance.GetOrAddHook<H_AgentCachedStats>().Reset();
			return true;
		}
	}
}