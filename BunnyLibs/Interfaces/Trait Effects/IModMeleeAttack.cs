using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using RogueLibsCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BunnyLibs
{
	public interface IModMeleeAttack
	{
		/// <summary><code>
		///		Weak                    0.50f
		///		Withdrawal              0.75f
		///		Strength (Small)        1.25f
		///		Strength                1.50f
		/// </code></summary>
		float MeleeDamage { get; }

		/// <summary><code>
		///		BigKnockbackForAll      1.50f
		///		KnockbackMore           1.50f
		///		KnockbackLess           1.50f
		///		KnockbackLess2          2.00f
		/// </code></summary>
		float MeleeKnockback { get; }

		/// <summary><code>
		///		Paralyzed               0.00f
		///		Melee Mobility          0.50f
		///		Long Lunge              1.50f
		///		Long Lunge +            1.80f
		/// </code></summary>
		float MeleeLunge { get; }

		//	See RHR's Bullet Mods for an easy way to get this (switch div/mul OpCodes)
		//float MeleePenetration { get; }

		/// <summary><code>
		/// Fist, Knife, Baton, Wrench  5.00f
		/// Sledge, Crowbar, Axe, Bat   4.00f
		/// </code></summary>
		float MeleeSpeed { get; } // This should tie into weapon weight eventually

		/// <summary>
		/// Whether to apply stat multipliers to this attack.
		/// </summary>
		bool ApplyModMeleeAttack();
		 
		bool CanHitGhost();

		void OnStrike(PlayfieldObject target); // TODO: User can pass true or false to cancel vanilla ChangeHealth

		/// <summary>
		/// Whether to apply Melee Mobility effect on swing. <br></br>
		/// Precedence: False, True, Null
		/// </summary>
		bool? SetMobility(); // Null for no opinion
	}

	[HarmonyPatch(typeof(Combat))]
	static class P_Combat_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		[HarmonyPostfix, HarmonyPatch("Start")]
		private static void NPCAggressiveness(Combat __instance, ref Agent ___agent)
		{
			if (___agent.isPlayer > 0)
				return;

			//logger.LogDebug($"Combat.Start Postfix: {___agent.agentRealName}");

			// TRY IF NEEDED:
			//__instance.meleeJustBlockedTimeStart = 0f;
			//__instance.meleeJustHitCloseTimeStart = 0f;
			//__instance.meleeJustHitTimeStart = 0f;

			float meleeSpeed = ___agent.GetHook<H_AgentCachedStats>().MeleeSpeed;
			__instance.meleeJustBlockedTimeStart *= meleeSpeed;
			__instance.meleeJustHitCloseTimeStart *= meleeSpeed;
			__instance.meleeJustHitTimeStart *= meleeSpeed;
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
			__instance.meleeContainerAnim.speed *= __instance.agent.GetHook<H_AgentCachedStats>().MeleeSpeed;
		}

		//	TODO: Move to Gun trait
		// I don't think this is actually used except in guns with NPCs
		//[HarmonyPostfix, HarmonyPatch(nameof(Melee.SetWeaponCooldown))]
		public static void RangedWeaponCooldown(Melee __instance)
		{
			float cooldownMult = __instance.agent.GetHook<H_AgentCachedStats>().MeleeSpeed;
			logger.LogDebug($"Setting Weapon Cooldown ({__instance.agent.agentRealName}): {__instance.agent.weaponCooldown}");
			__instance.agent.weaponCooldown = 0f;//*= cooldownMult; // No real difference observed
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

		internal static bool CanHitGhost(Agent striker, Agent target) =>
			target is null
				? false
				: !target.ghost || striker.GetTraits<IModMeleeAttack>().Any(t => t.CanHitGhost());

		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.HitAftermath))]
		private static IEnumerable<CodeInstruction> AllowGhostHit_01(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo ghost = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.ghost));
			FieldInfo myMelee = AccessTools.DeclaredField(typeof(MeleeHitbox), nameof(MeleeHitbox.myMelee));
			FieldInfo agent = AccessTools.DeclaredField(typeof(Melee), nameof(Melee.agent));
			MethodInfo canHitGhost = AccessTools.DeclaredMethod(typeof(P_MeleeHitbox_IModMeleeAttack), nameof(P_MeleeHitbox_IModMeleeAttack.CanHitGhost));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Ldfld, ghost),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, myMelee),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Ldloc_S, 8),
					new CodeInstruction(OpCodes.Call, canHitGhost),
					new CodeInstruction(OpCodes.Ldc_I4_1), // Bool-inverter
					new CodeInstruction(OpCodes.Xor),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.HitObject))]
		private static IEnumerable<CodeInstruction> AllowGhostHit_02(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo ghost = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.ghost));
			FieldInfo myMelee = AccessTools.DeclaredField(typeof(MeleeHitbox), nameof(MeleeHitbox.myMelee));
			FieldInfo agent = AccessTools.DeclaredField(typeof(Melee), nameof(Melee.agent));
			MethodInfo canHitGhost = AccessTools.DeclaredMethod(typeof(P_MeleeHitbox_IModMeleeAttack), nameof(P_MeleeHitbox_IModMeleeAttack.CanHitGhost));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_S, 8),
					new CodeInstruction(OpCodes.Ldfld, ghost),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, myMelee),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Ldloc_S, 8),
					new CodeInstruction(OpCodes.Call, canHitGhost),
					new CodeInstruction(OpCodes.Ldc_I4_1), // Bool-inverter
					new CodeInstruction(OpCodes.Xor),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.HitObject))]
		private static IEnumerable<CodeInstruction> AllowGhostHit_03(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo ghost = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.ghost));
			FieldInfo myMelee = AccessTools.DeclaredField(typeof(MeleeHitbox), nameof(MeleeHitbox.myMelee));
			FieldInfo agent = AccessTools.DeclaredField(typeof(Melee), nameof(Melee.agent));
			MethodInfo canHitGhost = AccessTools.DeclaredMethod(typeof(P_MeleeHitbox_IModMeleeAttack), nameof(P_MeleeHitbox_IModMeleeAttack.CanHitGhost));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_S, 26),
					new CodeInstruction(OpCodes.Ldfld, ghost),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, myMelee),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Ldloc_S, 8),
					new CodeInstruction(OpCodes.Call, canHitGhost),
					new CodeInstruction(OpCodes.Ldc_I4_1), // Bool-inverter
					new CodeInstruction(OpCodes.Xor),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}


		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.HitObject))]
		private static IEnumerable<CodeInstruction> CallApplyOnStrikeEffects(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			MethodInfo applyOnHitEffects = AccessTools.DeclaredMethod(typeof(P_MeleeHitbox_IModMeleeAttack), nameof(P_MeleeHitbox_IModMeleeAttack.ApplyOnStrikeEffects));
			FieldInfo myMelee = AccessTools.DeclaredField(typeof(MeleeHitbox), nameof(MeleeHitbox.myMelee));
			FieldInfo agent = AccessTools.DeclaredField(typeof(Melee), nameof(Melee.agent));
			FieldInfo zombified = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.zombified));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				prefixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Call) // Add
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, applyOnHitEffects),
				},
				postfixInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, myMelee),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Ldfld, zombified),
				});

			patch.ApplySafe(instructions, logger);
			return instructions;
		}

		// TODO: Transpiler
		private static void ApplyOnStrikeEffects(MeleeHitbox __instance, GameObject hitObject)
		{
			Agent striker = __instance.myMelee.agent;
			Agent target = hitObject.GetComponent<ObjectSprite>().agent;

			foreach (IModMeleeAttack trait in striker.GetTraits<IModMeleeAttack>())
				trait.OnStrike(target);
		}

		[HarmonyTranspiler, HarmonyPatch(nameof(MeleeHitbox.MeleeHitEffect))]
		private static IEnumerable<CodeInstruction> AllowGhostHit_04(IEnumerable<CodeInstruction> codeInstructions)
		{
			List<CodeInstruction> instructions = codeInstructions.ToList();
			FieldInfo ghost = AccessTools.DeclaredField(typeof(Agent), nameof(Agent.ghost));
			FieldInfo myMelee = AccessTools.DeclaredField(typeof(MeleeHitbox), nameof(MeleeHitbox.myMelee));
			FieldInfo agent = AccessTools.DeclaredField(typeof(Melee), nameof(Melee.agent));
			MethodInfo canHitGhost = AccessTools.DeclaredMethod(typeof(P_MeleeHitbox_IModMeleeAttack), nameof(P_MeleeHitbox_IModMeleeAttack.CanHitGhost));

			CodeReplacementPatch patch = new CodeReplacementPatch(
				pullNextLabelUp: false,
				expectedMatches: 1,
				targetInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Ldfld, ghost),
				},
				insertInstructions: new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, myMelee),
					new CodeInstruction(OpCodes.Ldfld, agent),
					new CodeInstruction(OpCodes.Ldloc_2),
					new CodeInstruction(OpCodes.Call, canHitGhost),
					new CodeInstruction(OpCodes.Ldc_I4_1), // Bool-inverter
					new CodeInstruction(OpCodes.Xor),
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

		[HarmonyPostfix, HarmonyPatch(nameof(Movement.FindKnockBackStrength))]
		public static void AdjustKnockBackStrength(PlayfieldObject ___playfieldObject, ref float __result)
		{
			// This might be done at the wrong place. Take note of WHICH agent is called in the original, ask why the hitter isn't and where they are.
			//logger.LogDebug($"PFO: {___playfieldObject}");
			//logger.LogDebug($"KBO: {___playfieldObject.knockedByObject}"); // Blank but not NRE
			//logger.LogDebug($"PFOA: {___playfieldObject.knockedByObject.playfieldObjectAgent}"); // NRE

			StackTrace stackTrace = new StackTrace();

			if (stackTrace.FrameCount > 1)
			{
				//logger.LogDebug($"FrameCount: {stackTrace.FrameCount}");
				MethodBase callingMethod = stackTrace.GetFrame(1).GetMethod();
				//logger.LogDebug($"Calling Method: {callingMethod.DeclaringType}.{callingMethod.Name}");
			}

			Agent? hitterAgent = ___playfieldObject.knockedByObject.playfieldObjectAgent ?? null;

			// TODO: Ensure this applies only to melee, since rn it applies to bullets too

			// TEST: Does this still NRE when charging into border walls?
			if (hitterAgent is null)
				return;

			__result *= hitterAgent.GetOrAddHook<H_AgentCachedStats>().MeleeKnockback;
		}

		// Movement.Knockback is only for non-agent hitters and MAYBE bullets.

		[HarmonyPrefix, HarmonyPatch(typeof(Movement), nameof(Movement.KnockForward))]
		private static bool ApplyLungeBonus(Movement __instance, ref float strength)
		{
			Agent agent = __instance.GetComponent<Agent>();
			float LungeBonus = agent.GetHook<H_AgentCachedStats>().MeleeLunge;
			strength *= LungeBonus;
			return true;
		}
	}

	[HarmonyPatch(typeof(PlayfieldObject))]
	static class P_PlayfieldObject_IModMeleeAttack
	{
		private static readonly ManualLogSource logger = BLLogger.GetLogger();
		private static GameController GC => GameController.gameController;

		// BUG: Disables damage effects from Giant/Diminutive. Addressing in Agent Stat Hook.
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
					new CodeInstruction(OpCodes.Callvirt),		//	getLocalScale, Seems to work with blank
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
		public static float ApplyMeleeDamage(Agent agent) => 
			agent.GetHook<H_AgentCachedStats>().MeleeDmg;

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