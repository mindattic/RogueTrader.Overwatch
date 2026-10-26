using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.AI;
using Kingmaker.AI.BehaviourTrees;
using Kingmaker.AI.BehaviourTrees.Nodes;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using UnityEngine;

namespace RogueTraderOverwatch;

// How enemies use Reflex Shot, Overwatch and Suppression. Left alone, the AI sorts these end-turn abilities last but
// still casts them on leftover AP (Suppression on any shootable enemy, Overwatch only at enemies already in the cone).
// Instead the normal ability search never sees them, and one rule decides, only once the unit is done moving:
//   - it hasn't attacked this turn (all its AP left) and holds a ranged weapon;
//   - it can shoot someone but every such enemy is in full cover: Suppression on the nearest one not already suppressed;
//   - it can shoot someone in the open: nothing here, the normal attack runs;
//   - it can shoot nobody, but an enemy is within weapon range + ApproachMargin cells: Overwatch aimed at the nearest
//     one with 2+ AP, Reflex Shot with 1 AP (one shot either way, and the circle covers every direction).
internal static class EnemyAi
{
    public const int ApproachMargin = 8;

    private static readonly string[] OurAbilities =
    {
        "27f7f769d0b94ab18fee0cde8a577a4d", // Reflex Shot
        OverwatchCone.AbilityGuid,
        "5db3949e7bf544bcb10cc3a2d51eb9ea", // Suppression
    };

    public static bool IsOurs(AbilityData ability) => ability?.Blueprint != null && OurAbilities.Contains(ability.Blueprint.AssetGuid);

    public static bool IsAiUnit(BaseUnitEntity unit) => unit != null && !unit.IsPlayerFaction;

    public static (AbilityData Ability, TargetWrapper Target) Decide(BaseUnitEntity unit, CastTimepointType timepoint)
    {
        if (!IsAiUnit(unit) || !TurnController.IsInTurnBasedCombat() || unit.LifeState.IsDead)
            return default;
        var combat = unit.CombatState;
        if (combat.ActionPointsYellow < 1 || combat.ActionPointsYellow < combat.WarhammerInitialAPYellow.ModifiedValue)
            return default;
        if (timepoint != CastTimepointType.AfterMove && combat.ActionPointsBlue >= 1)
            return default;
        var shot = unit.GetFirstWeapon()?.Abilities.FirstOrDefault()?.Data;
        if (shot == null || shot.Weapon == null || !shot.Weapon.Blueprint.IsRanged)
            return default;
        var here = (CustomGridNodeBase)unit.CurrentUnwalkableNode;
        if (here == null)
            return default;

        var enemies = Game.Instance.State.AllBaseAwakeUnits
            .Where(e => e != unit && !e.LifeState.IsDead && unit.IsEnemy(e) && e.IsInCombat)
            .ToList();
        var shootable = new List<(BaseUnitEntity Unit, int Distance, LosCalculations.CoverType Cover)>();
        foreach (var enemy in enemies)
        {
            var node = (CustomGridNodeBase)enemy.CurrentUnwalkableNode;
            if (node != null && shot.CanTargetFromNode(here, node, new TargetWrapper(enemy), out int distance, out var cover))
                shootable.Add((enemy, distance, cover));
        }

        if (shootable.Count > 0)
        {
            if (shootable.Any(s => s.Cover != LosCalculations.CoverType.Full))
                return default;
            var suppression = Usable(unit, "5db3949e7bf544bcb10cc3a2d51eb9ea");
            var suppressed = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Suppression.BuffGuid);
            var target = shootable.Where(s => suppressed == null || !s.Unit.Buffs.Contains(suppressed))
                .OrderBy(s => s.Distance).Select(s => s.Unit).FirstOrDefault();
            if (suppression == null || target == null)
                return default;
            var wrapper = new TargetWrapper(target);
            return suppression.CanTarget(wrapper) ? (suppression, wrapper) : default;
        }

        var overwatch = Usable(unit, OverwatchCone.AbilityGuid);
        int range = overwatch?.RangeCells ?? shot.RangeCells;
        var nearest = enemies
            .Select(e => (Unit: e, Node: (CustomGridNodeBase)e.CurrentUnwalkableNode))
            .Where(e => e.Node != null)
            .Select(e => (e.Unit, Distance: here.CellDistanceTo(e.Node)))
            .Where(e => e.Distance <= range + ApproachMargin)
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
        if (nearest.Unit == null)
            return default;

        if (overwatch != null && combat.ActionPointsYellow >= 2)
        {
            // Aim at the nearest enemy, no farther than weapon range: the cone resize patch sizes it from this distance.
            var from = unit.Position;
            var toward = nearest.Unit.Position - from;
            toward.y = 0;
            float cells = Mathf.Clamp(nearest.Distance, OverwatchCone.MinRadius, Math.Max(OverwatchCone.MinRadius, range));
            var point = from + toward.normalized * (cells * GraphParamsMechanicsCache.GridCellSize);
            var wrapper = new TargetWrapper(point);
            if (toward.sqrMagnitude > 0.01f && overwatch.CanTarget(wrapper))
                return (overwatch, wrapper);
        }

        var reflex = Usable(unit, "27f7f769d0b94ab18fee0cde8a577a4d");
        if (reflex != null)
        {
            // Face the nearest enemy, the direction it's expected from (the facing sets Reflex Shot's aim modifier).
            var toward = nearest.Unit.Position - unit.Position;
            toward.y = 0;
            var facing = toward.sqrMagnitude > 0.01f
                ? new TargetWrapper(unit.Position + toward.normalized * GraphParamsMechanicsCache.GridCellSize)
                : new TargetWrapper(unit);
            if (reflex.CanTarget(facing))
                return (reflex, facing);
        }
        return default;
    }

    private static AbilityData Usable(BaseUnitEntity unit, string guid)
    {
        var data = unit.Abilities.RawFacts.FirstOrDefault(a => a.Blueprint.AssetGuid == guid)?.Data;
        return data != null && data.IsAvailable ? data : null;
    }

    public static void Log(Exception ex, string where) => PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: " + where);
}

[HarmonyPatch(typeof(DecisionContext), nameof(DecisionContext.IsUsableAbility))]
internal static class EnemyAi_HideFromNormalSearch
{
    private static void Postfix(DecisionContext __instance, Ability ability, ref bool __result)
    {
        try
        {
            if (__result && EnemyAi.IsOurs(ability?.Data) && EnemyAi.IsAiUnit(__instance.Unit))
                __result = false;
        }
        catch (Exception ex)
        {
            EnemyAi.Log(ex, "hiding overwatch from the AI's ability search");
        }
    }
}

[HarmonyPatch(typeof(TaskNodeSelectAbilityTarget), "TickInternal")]
internal static class EnemyAi_ChooseOverwatch
{
    private static bool Prefix(TaskNodeSelectAbilityTarget __instance, Blackboard blackboard, ref Status __result)
    {
        try
        {
            var context = blackboard?.DecisionContext;
            var (ability, target) = EnemyAi.Decide(context?.Unit, __instance.CastTimepoint);
            if (ability == null)
                return true;
            context.Ability = ability;
            context.AbilityTarget = target;
            context.TryTargetAllInsteadOfHatedOnly = false;
            __result = Status.Success;
            return false;
        }
        catch (Exception ex)
        {
            EnemyAi.Log(ex, "choosing an enemy overwatch");
            return true;
        }
    }
}
