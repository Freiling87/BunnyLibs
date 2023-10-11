using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BunnyLibs
{
	public interface IModMeleeAttack
	{
		float MeleeDamage { get; }
		float MeleeKnockback { get; }
		float MeleeLunge { get; }
		float MeleeSpeed { get; } // This should tie into weapon weight eventually
		bool CanHitGhost();
		bool ApplyMeleeAttack();
		void OnStrike();
		bool? SetMobility(); // Null for no opinion
	}

	[HarmonyPatch(typeof(Combat))]
	static class P_Combat_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch("Start")]
		private static void Start_Postfix(Combat __instance, ref Agent ___agent)
		{
			logger.LogDebug($"Combat.Start Postfix: {___agent.agentRealName}");

			// TEST:
			//__instance.meleeJustBlockedTimeStart = 0f;
			//__instance.meleeJustHitCloseTimeStart = 0f;
			//__instance.meleeJustHitTimeStart = 0f;

			foreach (IModMeleeAttack trait in ___agent.GetTraits<IModMeleeAttack>().Where(t => t.ApplyMeleeAttack()))
			{
				// These are all parts of variable groups, selected by the name "Start."
				// This is a relatively static variable that is frequently applied as a cooldown timer to the others in the group.
				// Ultimately they should determine NPC attack cooldowns.

				// But they seem wrong - shouldn't this be division, not multiplication? 
				__instance.meleeJustBlockedTimeStart *= trait.MeleeSpeed;
				__instance.meleeJustHitCloseTimeStart *= trait.MeleeSpeed;
				__instance.meleeJustHitTimeStart *= trait.MeleeSpeed;

				logger.LogDebug($"Applying {trait}, SpeedMult: {trait.MeleeSpeed}");
				logger.LogDebug(
					$"\tmeleeJustBlockedTimeStart:  {__instance.meleeJustBlockedTimeStart}\n" +
					$"\tmeleeJustHitCloseTimeStart: {__instance.meleeJustHitCloseTimeStart}\n" +
					$"\tmeleeJustHitTimeStart:      {__instance.meleeJustHitTimeStart}\n");
			}
		}
	}

	[HarmonyPatch(typeof(Melee))]
	static class P_Melee_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(typeof(Melee), nameof(Melee.Attack), new[] { typeof(bool) })]
		private static void ApplyMobility(Melee __instance)
		{
			Agent agent = __instance.agent;
			List<IModMeleeAttack> mobilityTraits = agent.GetTraits<IModMeleeAttack>().ToList();

			// Leaving vanilla melee mobility effect in place. It's overpowered and neglects unarmed in general, but the addition of unarmed styles will mean any of those will be preferable to a melee character, and can disallow mobility ad-hoc.
			//if (agent.HasTrait(VanillaTraits.FloatsLikeButterfly) && agent.GetHook<H_Agent>().EquippedItem.invItemName == vItem.Fist)
			//	agent.melee.canMove = false;

			if (mobilityTraits.Any(t => t.SetMobility() == false))
				agent.melee.canMove = false;
			else if (mobilityTraits.Any(t => t.SetMobility() == true)) // Must be exclusive to negations
				agent.melee.canMove = true;
		}

		[HarmonyPostfix, HarmonyPatch(nameof(Melee.Attack), new[] { typeof(bool) })]
		public static void SetAttackSpeed(Melee __instance)
		{
			foreach (IModMeleeAttack trait in __instance.agent.GetTraits<IModMeleeAttack>())
				__instance.meleeContainerAnim.speed *= trait.MeleeSpeed;
		}

		//	TODO: Move to Gun trait
		// I don't think this is actually used except in guns
		//[HarmonyPostfix, HarmonyPatch(nameof(Melee.SetWeaponCooldown))]
		public static void SetWeaponCooldown_Postfix(Melee __instance)
		{
			Agent agent = __instance.agent;

			foreach (IModMeleeAttack trait in agent.GetTraits<IModMeleeAttack>())
			{
				logger.LogDebug($"Setting Weapon Cooldown ({agent.agentRealName}): {agent.weaponCooldown}");
				agent.weaponCooldown = 0; ///= trait.CooldownSpeedupMultiplier; // No real difference observed
			}
		}
	}

	[HarmonyPatch(typeof(MeleeHitbox))] 
	static class P_MeleeHitbox_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.HitObject))]
		private static IEnumerable<CodeInstruction> BlockGiantAutoDemolish(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo agentSpriteTransform = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.agentSpriteTransform));
			FieldInfo x = AccessTools.DeclaredField(typeof(Vector3), nameof(Vector3.x));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldfld, x),
				},
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_R4, 1f),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_R4, 2f),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
	}

	[HarmonyPatch(typeof(Movement))]
	static class P_Movement_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPrefix, HarmonyPatch(typeof(Movement), nameof(Movement.KnockForward))]
		private static bool ApplyLungeBonus(Movement __instance, ref float strength)
		{
			Agent agent = __instance.GetComponent<Agent>();
			float LungeBonus = agent.GetHook<H_AgentStats>().StatMeleeLunge;
			strength *= LungeBonus;
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch(nameof(PlayfieldObject.FindDamage), new[] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) })]
		private static void LogNetDamage(PlayfieldObject damagerObject, ref int __result)
		{
			damagerObject.TryGetComponent(out Agent agent);

			if (agent is null)
				return;

			H_AgentStats hook = agent.GetHook<H_AgentStats>();
			logger.LogDebug($"{agent.agentRealName} NetMeleeDamage: {__result}");

		}

		[HarmonyTranspiler, HarmonyPatch(nameof(PlayfieldObject.FindDamage), new[] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) })]
		private static IEnumerable<CodeInstruction> SetMeleeDamageDealt(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo agentSpriteTransform = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.agentSpriteTransform));
			FieldInfo x = AccessTools.DeclaredField(typeof(Vector3), nameof(Vector3.x));
			MethodInfo damageMultiplier = AccessTools.DeclaredMethod(typeof(P_PlayfieldObject_IModMeleeAttack), nameof(P_PlayfieldObject_IModMeleeAttack.ApplyMeleeDamage));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_S, 6),	//	agent
					new CodeInstruction(OpCodes.Ldfld, agentSpriteTransform),
					new CodeInstruction(OpCodes.Callvirt),//, getLocalScale), Try blank first
					new CodeInstruction(OpCodes.Ldfld, x),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_S, 6),
					new CodeInstruction(OpCodes.Call, damageMultiplier),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Stloc_S, 22),
					new CodeInstruction(OpCodes.Ldloc_S, 7),
					new CodeInstruction(OpCodes.Ldloc_S, 22),
					new CodeInstruction(OpCodes.Mul),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
		public static float ApplyMeleeDamage(Agent agent)
		{
			return agent.GetHook<H_AgentStats>().StatMeleeDmg;
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(PlayfieldObject.FindDamage), new[] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) })]
		private static IEnumerable<CodeInstruction> BlockGiantAutokill(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo agentSpriteTransform = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.agentSpriteTransform));
			FieldInfo x = AccessTools.DeclaredField(typeof(Vector3), nameof(Vector3.x));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldfld, x),
				},
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_R4, 1f),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldc_R4, 2f),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}
	}
}