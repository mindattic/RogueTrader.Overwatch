using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.PatternAttack;
using Kingmaker.UnitLogic.Abilities.Components.Patterns;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;

namespace RogueTraderOverwatch;

// The Overwatch ability: a cone sized by the cursor, one shot per AP left at cast, fire spread across enemies.
internal static class OverwatchCone
{
    public const string AbilityGuid = "b5855687981e4ba9a7dfc8c4af03229f";

    public const int MinRadius = 3;
    public const int NearAngle = 90;
    public const int FarAngle = 20;
    public const int MinReshotDistance = 2;

    public static bool IsOurs(AbilityData ability) => ability?.Blueprint?.AssetGuid == AbilityGuid;

    public static int RadiusFor(int distance, int range) => Mathf.Clamp(distance, MinRadius, Math.Max(MinRadius, range));

    public static AoEPattern ConeFor(int distance, int range)
    {
        int radius = RadiusFor(distance, range);
        int max = Math.Max(MinRadius, range);
        float t = max == MinRadius ? 1f : (radius - MinRadius) / (float)(max - MinRadius);
        int angle = Mathf.RoundToInt(Mathf.Lerp(NearAngle, FarAngle, t));
        return AoEPattern.Cone(angle, radius);
    }

    // The cone's locked-in radius at cast, keyed by caster UniqueId: how far it reaches decides the snap-shot
    // penalty (OverwatchDebuff.PenaltyFor). Captured alongside the live preview resize, so the last value before
    // cast is whatever the player clicked.
    public static readonly Dictionary<string, int> LockedRadius = new();

    // Shots left are the rank of this buff, which the engine adds and removes with the overwatch
    // (AbilityEffectOverwatch.ApplyingBuffs) and saves with the unit.
    public const string ShotsBuffGuid = "e5ce47452a6d47d78e88178f4ee7bba5";

    public static Buff ShotsBuff(BaseUnitEntity unit) =>
        unit.Buffs.GetBuff(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(ShotsBuffGuid));

    internal sealed class Watch
    {
        public readonly Dictionary<string, CustomGridNodeBase> LastShotAt = new();
    }

    // Keyed by the overwatcher's UniqueId. Only the re-shot positions live here (not saved): after a
    // reload each enemy in the cone counts as not yet shot.
    public static readonly Dictionary<string, Watch> Watches = new();
    public static readonly Dictionary<string, int> PendingShots = new();

    public static void Log(Exception ex, string where) => PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: " + where);
}

[HarmonyPatch(typeof(AbilityAoEPatternSettings), nameof(AbilityAoEPatternSettings.GetOrientedPattern))]
internal static class OverwatchCone_ResizeByCursor
{
    // One seam for both the targeting preview and AbilityEffectOverwatch.GetOverwatchArea, so the locked zone matches the preview.
    private static bool Prefix(AbilityAoEPatternSettings __instance, IAbilityDataProviderForPattern ability, CustomGridNodeBase casterNode, CustomGridNodeBase targetNode, bool coveredTargetsOnly, ref OrientedPatternData __result)
    {
        try
        {
            if (!OverwatchCone.IsOurs(ability?.Data) || casterNode == null || targetNode == null)
                return true;
            int distance = casterNode.CellDistanceTo(targetNode);
            if (ability.Caster is BaseUnitEntity unit)
                OverwatchCone.LockedRadius[unit.UniqueId] = OverwatchCone.RadiusFor(distance, ability.RangeCells);
            var pattern = OverwatchCone.ConeFor(distance, ability.RangeCells);
            __result = AoEPatternHelper.GetOrientedPattern(ability, ability.Caster, pattern, __instance, casterNode, targetNode, __instance.CastOnSameLevel, true, coveredTargetsOnly, out _);
            if (ability.Caster is BaseUnitEntity previewUnit)
                AbilityOutline.ShowPreview(previewUnit, __result);
            return false;
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "resizing the cone");
            return true;
        }
    }
}

[HarmonyPatch(typeof(UnitUseAbility), "OnStart")]
internal static class OverwatchCone_CaptureActionPoints
{
    // AP is read when the command starts, before UnitUseAbility spends the cost.
    private static void Prefix(UnitUseAbility __instance)
    {
        try
        {
            if (OverwatchCone.IsOurs(__instance.Ability) && __instance.Executor is BaseUnitEntity unit)
                OverwatchCone.PendingShots[unit.UniqueId] = unit.CombatState.ActionPointsYellow;
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "reading action points");
        }
    }
}

[HarmonyPatch(typeof(AbilityEffectOverwatch), nameof(AbilityEffectOverwatch.Apply))]
internal static class OverwatchCone_StartBudget
{
    private static void Postfix(AbilityExecutionContext context)
    {
        try
        {
            if (!OverwatchCone.IsOurs(context?.Ability) || context.MaybeCaster is not BaseUnitEntity unit)
                return;
            if (!OverwatchCone.PendingShots.TryGetValue(unit.UniqueId, out int shots))
                shots = unit.CombatState.ActionPointsYellow;
            OverwatchCone.PendingShots.Remove(unit.UniqueId);
            OverwatchCone.Watches[unit.UniqueId] = new OverwatchCone.Watch();
            var buff = OverwatchCone.ShotsBuff(unit);
            if (buff != null)
                buff.AddRank(Math.Max(1, shots) - buff.Rank);
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "starting the shot budget");
        }
    }
}

[HarmonyPatch(typeof(PartOverwatch), nameof(PartOverwatch.TryTriggerAttack))]
internal static class OverwatchCone_ChooseShots
{
    private static bool Prefix(PartOverwatch __instance, BaseUnitEntity target, out bool __state)
    {
        __state = false;
        try
        {
            if (!OverwatchCone.IsOurs(__instance.Ability))
                return true;
            var owner = (BaseUnitEntity)__instance.Owner;
            if (!OverwatchCone.Watches.TryGetValue(owner.UniqueId, out var watch))
                OverwatchCone.Watches[owner.UniqueId] = watch = new OverwatchCone.Watch();
            if (OverwatchCone.ShotsBuff(owner) is not { Rank: > 0 })
            {
                __instance.Stop();
                return false;
            }

            var here = (CustomGridNodeBase)target.CurrentUnwalkableNode;
            if (watch.LastShotAt.TryGetValue(target.UniqueId, out var lastNode))
            {
                // Standing still (acting in place) or creeping under the minimum never draws a repeat shot.
                if (here == null || lastNode == null || here.CellDistanceTo(lastNode) < OverwatchCone.MinReshotDistance)
                    return false;
                if (AnyUnshotEnemyInZone(__instance, owner, watch))
                    return false;
            }

            watch.LastShotAt[target.UniqueId] = here;
            __state = true;
            return true;
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "choosing an overwatch shot");
            return false;
        }
    }

    private static void Postfix(PartOverwatch __instance, bool __state)
    {
        try
        {
            if (!__state)
                return;
            // The last shot stops the overwatch, which also removes the buff; otherwise spend one rank.
            var buff = OverwatchCone.ShotsBuff((BaseUnitEntity)__instance.Owner);
            if (buff == null || buff.Rank <= 1)
                __instance.Stop();
            else
                buff.RemoveRank(1);
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "ending overwatch");
        }
    }

    private static bool AnyUnshotEnemyInZone(PartOverwatch part, BaseUnitEntity owner, OverwatchCone.Watch watch)
    {
        foreach (var unit in Game.Instance.State.AllBaseAwakeUnits)
        {
            if (unit.LifeState.IsDeadOrUnconscious || !owner.IsEnemy(unit) || watch.LastShotAt.ContainsKey(unit.UniqueId))
                continue;
            if (part.Contains(unit))
                return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(PartOverwatch), nameof(PartOverwatch.Stop))]
internal static class OverwatchCone_ForgetOnStop
{
    private static void Prefix(PartOverwatch __instance)
    {
        try
        {
            if (OverwatchCone.IsOurs(__instance.Ability))
                OverwatchCone.Watches.Remove(__instance.Owner.UniqueId);
        }
        catch (Exception ex)
        {
            OverwatchCone.Log(ex, "clearing overwatch state");
        }
    }
}
