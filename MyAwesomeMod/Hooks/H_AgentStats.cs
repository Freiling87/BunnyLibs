using BepInEx.Logging;
using HarmonyLib;
using RogueLibsCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BunnyLibs
{
	public class H_AgentStats : HookBase<PlayfieldObject>
	{
		// NOTE: Instance = host Agent

		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		public static GameController GC => GameController.gameController;

		Agent agent => (Agent)Instance;
		public InvItem EquippedItem =>
			agent.inventory.equippedSpecialAbility ?? agent.inventory.equippedWeapon ?? agent.inventory.fist;

		protected override void Initialize()
		{
		}
		public void Reset()
		{
			AgentBodySizeHelper.ApplySize(agent);
		}

		//	PHYSIQUE SYSTEM
		public void LogStatSheet()
		{
		}

		//	IModHealth
		public int StatHealthPerEndurance =>
			(int)GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcVigor)}", CalcVigor);
		private float CalcVigor()
		{
			int vigor = 20;

			foreach (IModHealthPerEndurance trait in agent.GetTraits<IModHealthPerEndurance>())
			{
				vigor += trait.HealthPerEnduranceBonus;
			}

			return vigor;
		}

		//	IModMeleeAttack
		public float StatMeleeDmg =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeDmg)}", CalcMeleeDmg);
		private float CalcMeleeDmg()
		{
			float damageMultiplier = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyMeleeAttack()))
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.MeleeDamage}");
				damageMultiplier *= trait.MeleeDamage;
			}

			return damageMultiplier;
		}
		public float StatMeleeKnockback =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeKnockback)}", CalcMeleeKnockback);
		private float CalcMeleeKnockback()
		{
			float strength = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyMeleeAttack()))
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.MeleeKnockback}");
				strength *= trait.MeleeKnockback;
			}

			return strength;
		}
		public float StatMeleeLunge =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeLunge)}", CalcMeleeLunge);
		private float CalcMeleeLunge()
		{
			float lunge = 1f;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyMeleeAttack()))
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.MeleeLunge}");
				lunge *= trait.MeleeLunge;
			}

			return lunge;
		}

		//	IModMoveSpeed
		public float StatAcceleration =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcAcceleration)}", CalcAcceleration);
		private float CalcAcceleration()
		{
			float accelerationMult = 1f;

			foreach (IModMoveSpeed trait in agent.GetTraits<IModMoveSpeed>())
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.Acceleration}");
				accelerationMult *= trait.Acceleration;
			}

			return accelerationMult;
		}
		public float StatMaxSpeed =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMaxSpeed)}", CalcMaxSpeed);
		private float CalcMaxSpeed()
		{
			float maxSpeedMult = 1f;

			foreach (IModMoveSpeed trait in agent.GetTraits<IModMoveSpeed>())
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.MoveSpeedMax}");
				maxSpeedMult *= trait.MoveSpeedMax;
			}

			return maxSpeedMult;
		}

		//	IModResistances
		public float StatMeleeRes =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcMeleeRes)}", CalcMeleeRes);
		private float CalcMeleeRes()
		{
			float meleeRes = 1f;

			foreach (IModResistances trait in agent.GetTraits<IModResistances>())
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.ResistMelee}");

				meleeRes *= trait.ResistMelee;
			}

			return meleeRes;
		}
		public float StatKnockbackRes =>
			GetCachedStat($"{agent.agentOriginalName}_{agent.agentID}_{nameof(CalcKnockbackRes)}", CalcKnockbackRes);
		private float CalcKnockbackRes()
		{
			float knockback = 1f;

			foreach (IModResistances trait in agent.GetTraits<IModResistances>())
			{
				logger.LogDebug($"Applying bonus ({trait}): {trait.ResistKnockback}");
				knockback *= trait.ResistKnockback;
			}

			return knockback;
		}

		//	Cache Mgr.
		private static Dictionary<string, Tuple<float, float>> statCache = new Dictionary<string, Tuple<float, float>>();
		private static readonly int cacheTTLSeconds = 1;

		public static float GetCachedStat(string cacheUID, Func<float> calculateStat)
		{
			lock (statCache)
			{
				if (statCache.TryGetValue(cacheUID, out var cacheEntry) && ((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds - cacheEntry.Item2) <= cacheTTLSeconds)
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
	public class P_Agent_AgentStatsHook
	{
		[HarmonyPrefix, HarmonyPatch("Start")]
		public static bool Start_CreateHook(Agent __instance)
		{
			__instance.GetOrAddHook<H_AgentStats>().Reset();
			return true;
		}
	}
}