using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules.Modifiers;
using HarmonyLib;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.PatternAttack;
using Kingmaker.UnitLogic.Abilities.Components.Patterns;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using RogueTrader.Shared;
using UnityEngine;

namespace RogueTraderOverwatch;

// Reflex Shot: a circle around the shooter, all directions, aimed toward a facing the player picks. Its shot gets an
// aim modifier from how far the shooter has to turn: +AimBonusAhead dead ahead, falling linearly to -AimPenaltyBehind
// for a full 180-degree spin (0 at 45, -10 at 90, -20 at 135 with the defaults).
internal static class ReflexShot
{
    public const int AimBonusAhead = 10;
    public const int AimPenaltyBehind = 30;

    // A reflex shot is a reaction, not an aimed shot: its hit chance starts from a flat BaseHitChance instead of the
    // shooter's Ballistic Skill + 30 (range falloff, the turn modifier, cover, talents and the weapon's own bonus still
    // apply on top), and it deals DamagePercent of the weapon's damage (damage modifiers still apply).
    public const int BaseHitChance = 33;
    public const int DamagePercent = 33;

    public static bool IsOurs(AbilityData ability) => ability?.Blueprint?.AssetGuid == Abilities.ReflexShot;

    public static int AimModifier(float degrees) =>
        Mathf.RoundToInt(AimBonusAhead - (AimBonusAhead + AimPenaltyBehind) * Mathf.Clamp(degrees, 0f, 180f) / 180f);

    // The modifier a Reflex Shot from this shooter, facing facingPoint, would get against target.
    public static int TotalModifier(BaseUnitEntity shooter, Vector3 facingPoint, Vector3 targetPosition) =>
        AimModifier(TurnAngle(shooter, facingPoint, targetPosition));

    // The real attack rolls a Reflex Shot made, and their hit-chance rules, each mapped to the Reflex Shot fact: the
    // base hit chance and damage patches below key off them. Weak, so finished rules are collected as usual.
    public static readonly ConditionalWeakTable<object, EntityFact> Rolls = new();

    public static void Mark(object rule, EntityFact source)
    {
        Rolls.Remove(rule);
        Rolls.Add(rule, source);
    }

    // The Reflex Shot fact a hit-chance or damage rule belongs to, or null: a marked real roll, or the hit-chance
    // preview working out a Reflex Shot for this shooter.
    public static EntityFact SourceOf(object rule, MechanicEntity initiator)
    {
        if (rule != null && Rolls.TryGetValue(rule, out var source))
            return source;
        return HitChancePreview.Pending is { Reflex: true } pending && pending.Shooter == initiator ? pending.Source : null;
    }

    // The angle between the facing (shooter toward the clicked point) and the shooter-to-target direction, on the ground.
    public static float TurnAngle(BaseUnitEntity shooter, Vector3 facingPoint, Vector3 targetPosition)
    {
        var facing = Flat(facingPoint - shooter.Position);
        if (facing.sqrMagnitude < 0.01f)
            facing = Flat(shooter.OrientationDirection);
        var toTarget = Flat(targetPosition - shooter.Position);
        return toTarget.sqrMagnitude < 0.01f ? 0f : Vector3.Angle(facing, toTarget);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    internal readonly struct PendingShot
    {
        public readonly string TargetId;
        public readonly int Modifier;
        public readonly EntityFact Source;
        public readonly float At;

        public PendingShot(string targetId, int modifier, EntityFact source, float at)
        {
            TargetId = targetId;
            Modifier = modifier;
            Source = source;
            At = at;
        }
    }

    // Overwatch mode stops the part as soon as the shot is queued, before the attack rolls, so the modifier is worked out
    // at the trigger and kept here (by shooter) until that attack rolls. Every roll at that target within PendingSeconds
    // uses it (a burst weapon rolls once per round); after that it lapses, so no later attack picks it up.
    public const float PendingSeconds = 5f;
    public static readonly Dictionary<string, PendingShot> Pending = new();

    public static readonly AccessTools.FieldRef<PartOverwatch, TargetWrapper> TargetField =
        AccessTools.FieldRefAccess<PartOverwatch, TargetWrapper>("m_Target");
}

// The circle stays centred on the shooter wherever the player clicks; the click only sets the facing. Same seam as the
// cone, so the preview and AbilityEffectOverwatch.GetOverwatchArea agree.
[HarmonyPatch(typeof(AbilityAoEPatternSettings), nameof(AbilityAoEPatternSettings.GetOrientedPattern))]
internal static class ReflexShot_CentreOnShooter
{
    private static bool Prefix(AbilityAoEPatternSettings __instance, IAbilityDataProviderForPattern ability, CustomGridNodeBase casterNode, CustomGridNodeBase targetNode, bool coveredTargetsOnly, ref OrientedPatternData __result)
    {
        try
        {
            if (!ReflexShot.IsOurs(ability?.Data) || casterNode == null)
                return true;
            var pattern = ((IAbilityAoEPatternProvider)__instance).Pattern;
            // The circle's size comes from the pattern, not the ability range: it's the weapon's full range.
            var attack = Abilities.RangedAttack(ability.Caster);
            if (attack != null && attack.RangeCells > 0 && attack.RangeCells != pattern.Radius)
                pattern = AoEPattern.CopyAndOverrideRadius(pattern, attack.RangeCells);
            __result = AoEPatternHelper.GetOrientedPattern(ability, ability.Caster, pattern, __instance, casterNode, casterNode, __instance.CastOnSameLevel, false, coveredTargetsOnly, out _);
            if (ability.Caster is BaseUnitEntity caster)
                AbilityOutline.ShowPreview(caster, __result, AbilityOutline.ReflexColor);
            return false;
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "centring Reflex Shot on the shooter");
            return true;
        }
    }
}

[HarmonyPatch(typeof(PartOverwatch), nameof(PartOverwatch.TryTriggerAttack))]
internal static class ReflexShot_RecordTurn
{
    private static void Prefix(PartOverwatch __instance, BaseUnitEntity target)
    {
        try
        {
            if (!ReflexShot.IsOurs(__instance.Ability) || __instance.Owner is not BaseUnitEntity shooter || target == null)
                return;
            var facing = ReflexShot.TargetField(__instance);
            if (facing == null)
                return;
            int modifier = ReflexShot.TotalModifier(shooter, facing.Point, target.Position);
            ReflexShot.Pending[shooter.UniqueId] = new ReflexShot.PendingShot(target.UniqueId, modifier, __instance.Ability.Fact, Time.time);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "working out the Reflex Shot turn");
        }
    }
}

// Adds the modifier, under the Reflex Shot ability's name, to the hit chance of that one attack roll. Hooked on the real
// roll (RulePerformAttackRoll builds its own hit-chance rule and triggers it first thing), so UI hit-chance previews
// never see or use it up, and it holds even when the attack command has already ended by the time the roll happens.
[HarmonyPatch(typeof(RulePerformAttackRoll), nameof(RulePerformAttackRoll.OnTrigger))]
internal static class ReflexShot_AimModifier
{
    private static void Prefix(RulePerformAttackRoll __instance)
    {
        try
        {
            if (ReflexShot.Pending.Count == 0 || __instance.Initiator is not BaseUnitEntity shooter || __instance.Target is not BaseUnitEntity target)
                return;
            if (!ReflexShot.Pending.TryGetValue(shooter.UniqueId, out var shot) || shot.TargetId != target.UniqueId)
                return;
            // Consumed here, win or lose: a Reflex Shot fires once (HitOnce), so nothing should reuse this entry,
            // and a later unrelated attack at the same target must not inherit its base chance, damage cut or modifier.
            ReflexShot.Pending.Remove(shooter.UniqueId);
            if (Time.time - shot.At > ReflexShot.PendingSeconds || shot.Source == null)
                return;
            ReflexShot.Mark(__instance, shot.Source);
            ReflexShot.Mark(__instance.HitChanceRule, shot.Source);
            if (shot.Modifier != 0)
                __instance.HitChanceRule.HitChanceValueModifiers.Add(shot.Modifier, shot.Source);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "adding the Reflex Shot aim modifier");
        }
    }
}

// A Reflex Shot's base hit chance is BaseHitChance instead of Ballistic Skill + 30: the end of the game's own ranged
// calculation (OnTriggerRanged) redone with that base, the same range falloff, modifiers and caps. Its overkill crit
// bonus is redone too, since the old base decided it.
[HarmonyPatch(typeof(RuleCalculateHitChances), "OnTriggerRanged")]
internal static class ReflexShot_BaseHitChance
{
    private static readonly MethodInfo SetBase = AccessTools.PropertySetter(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.ResultBaseChancesBeforeRecoil));
    private static readonly MethodInfo SetRaw = AccessTools.PropertySetter(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.RawResult));
    private static readonly MethodInfo SetNoLimit = AccessTools.PropertySetter(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.ResultHitChanceNoUpperLimit));
    private static readonly MethodInfo SetResult = AccessTools.PropertySetter(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.ResultHitChance));

    private static void Postfix(RuleCalculateHitChances __instance)
    {
        try
        {
            if (ReflexShot.SourceOf(__instance, (MechanicEntity)__instance.Initiator) == null || __instance.IsScatter)
                return;
            // Destructibles get the border chance from the game; leave that as it is.
            if (!__instance.Ability.IsBurstAttack && __instance.Target is DestructibleEntity)
                return;
            int baseChance = __instance.DistanceFactor > 0f ? Mathf.FloorToInt(ReflexShot.BaseHitChance * __instance.DistanceFactor) : 0;
            SetBase.Invoke(__instance, new object[] { baseChance });
            SetRawResult(__instance, Mathf.RoundToInt((baseChance + __instance.HitChanceValueModifiers.Value) * __instance.OverpenetrationModifier));
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "setting Reflex Shot's base hit chance");
        }
    }

    // Replaces a ranged hit-chance rule's raw result the way OnTriggerRanged sets it: the capped chance, and the
    // overkill crit bonus (the part over the cap), which the old raw value decided.
    public static void SetRawResult(RuleCalculateHitChances rule, int raw)
    {
        int border = rule.HitChanceBorderRule.ResultOverkillBorder;
        SetRaw.Invoke(rule, new object[] { raw });
        SetNoLimit.Invoke(rule, new object[] { Math.Max(0, raw) });
        SetResult.Invoke(rule, new object[] { Mathf.Clamp(raw, 0, border) });
        var fury = rule.RighteousFuryChanceRule.ChanceModifiers;
        fury.RemoveAll(m => m.Descriptor == ModifierDescriptor.HitChanceOverkill);
        int overkill = Mathf.RoundToInt(Mathf.Max(0, raw - border) * rule.CritChanceOverkillPercentMultipliers.Value);
        fury.Add(overkill, rule, ModifierDescriptor.HitChanceOverkill);
    }
}

// A Reflex Shot deals DamagePercent of the weapon's damage, under the Reflex Shot ability's name, alongside the game's
// own damage multipliers. Added to the rule's modifier holder before it runs, which the result copies.
[HarmonyPatch(typeof(RuleCalculateDamage), nameof(RuleCalculateDamage.OnTrigger))]
internal static class ReflexShot_Damage
{
    private static void Prefix(RuleCalculateDamage __instance)
    {
        try
        {
            var source = ReflexShot.SourceOf(__instance.RollPerformAttackRule, __instance.ConcreteInitiator);
            if (source != null)
                __instance.ValueModifiers.Add(ModifierType.PctMul_Extra, ReflexShot.DamagePercent, source);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "reducing Reflex Shot's damage");
        }
    }
}

// A new overwatch of any kind replaces whatever Reflex Shot was waiting to roll.
[HarmonyPatch(typeof(AbilityEffectOverwatch), nameof(AbilityEffectOverwatch.Apply))]
internal static class ReflexShot_ForgetOnNewOverwatch
{
    private static void Prefix(AbilityExecutionContext context)
    {
        try
        {
            if (context?.MaybeCaster is BaseUnitEntity caster)
                ReflexShot.Pending.Remove(caster.UniqueId);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "clearing a Reflex Shot");
        }
    }
}

// The facing point can be picked anywhere within the weapon's own range, matching the circle's radius.
[HarmonyPatch(typeof(RuleCalculateAbilityRange), nameof(RuleCalculateAbilityRange.OnTrigger))]
internal static class ReflexShot_RangeFromWeapon
{
    private static readonly MethodInfo SetResult = AccessTools.PropertySetter(typeof(RuleCalculateAbilityRange), nameof(RuleCalculateAbilityRange.Result));

    private static void Postfix(RuleCalculateAbilityRange __instance)
    {
        try
        {
            var ability = __instance.Ability;
            if (!ReflexShot.IsOurs(ability))
                return;
            var attack = Abilities.RangedAttack(ability.Caster);
            if (attack != null && attack.RangeCells > 0 && attack.RangeCells != __instance.Result)
                SetResult.Invoke(__instance, new object[] { attack.RangeCells });
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "setting Reflex Shot's range from the weapon");
        }
    }
}
