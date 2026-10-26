using System;
using System.Collections.Generic;
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
using Kingmaker.UnitLogic.Commands;
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

    public static bool IsOurs(AbilityData ability) => ability?.Blueprint?.AssetGuid == Abilities.ReflexShot;

    public static int AimModifier(float degrees) =>
        Mathf.RoundToInt(AimBonusAhead - (AimBonusAhead + AimPenaltyBehind) * Mathf.Clamp(degrees, 0f, 180f) / 180f);

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

        public PendingShot(string targetId, int modifier, EntityFact source)
        {
            TargetId = targetId;
            Modifier = modifier;
            Source = source;
        }
    }

    // Overwatch mode stops the part as soon as the shot is queued, before the attack rolls, so the modifier is worked out
    // at the trigger and kept here (by shooter) until that attack's hit chance is calculated.
    public static readonly Dictionary<string, PendingShot> Pending = new();

    public static readonly AccessTools.FieldRef<PartOverwatch, TargetWrapper> TargetField =
        AccessTools.FieldRefAccess<PartOverwatch, TargetWrapper>("m_Target");
}

// The circle stays centred on the shooter wherever the player clicks; the click only sets the facing. Same seam as the
// cone, so the preview and AbilityEffectOverwatch.GetOverwatchArea agree.
[HarmonyPatch(typeof(AbilityAoEPatternSettings), nameof(AbilityAoEPatternSettings.GetOrientedPattern))]
internal static class ReflexShot_CentreOnShooter
{
    private static bool Prefix(AbilityAoEPatternSettings __instance, IAbilityDataProviderForPattern ability, CustomGridNodeBase casterNode, bool coveredTargetsOnly, ref OrientedPatternData __result)
    {
        try
        {
            if (!ReflexShot.IsOurs(ability?.Data) || casterNode == null)
                return true;
            var pattern = ((IAbilityAoEPatternProvider)__instance).Pattern;
            __result = AoEPatternHelper.GetOrientedPattern(ability, ability.Caster, pattern, __instance, casterNode, casterNode, __instance.CastOnSameLevel, false, coveredTargetsOnly, out _);
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
            int modifier = ReflexShot.AimModifier(ReflexShot.TurnAngle(shooter, facing.Point, target.Position));
            ReflexShot.Pending[shooter.UniqueId] = new ReflexShot.PendingShot(target.UniqueId, modifier, __instance.Ability.Fact);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "working out the Reflex Shot turn");
        }
    }
}

// Adds the modifier, under the Reflex Shot ability's name, to the hit chance of that one overwatch attack.
[HarmonyPatch(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.OnTrigger))]
internal static class ReflexShot_AimModifier
{
    private static void Prefix(RuleCalculateHitChances __instance)
    {
        try
        {
            if (ReflexShot.Pending.Count == 0 || __instance.Initiator is not BaseUnitEntity shooter || __instance.Target is not BaseUnitEntity target)
                return;
            if (!ReflexShot.Pending.TryGetValue(shooter.UniqueId, out var shot) || shot.TargetId != target.UniqueId)
                return;
            if (shooter.Commands.Current is not UnitOverwatchAttack || shot.Source == null || shot.Modifier == 0)
                return;
            __instance.HitChanceValueModifiers.Add(shot.Modifier, shot.Source);
        }
        catch (Exception ex)
        {
            ModLog.Exception(ex, "adding the Reflex Shot aim modifier");
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
