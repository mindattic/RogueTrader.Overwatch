using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;

namespace RogueTraderOverwatch;

// Overwatched: a visual marker (Overwatched_Buff) on every enemy currently standing in an active Overwatch cone,
// whose rank is the snap-shot hit-chance penalty a shot from that cone carries, on top of the weapon's normal
// range falloff. Added, updated and removed live every tick (OverwatchController.Tick), not by the engine; when
// several overwatchers cover the same enemy, the rank shown is the worst (highest) of their penalties.
internal static class OverwatchDebuff
{
    public const string BuffGuid = "30f01920a70d4ec28860d0bd5e4193e3";
    public const int MaxPenalty = 30; // same magnitude as Reflex Shot's own max aim penalty

    public static int PenaltyFor(int radius, int range)
    {
        int max = Math.Max(OverwatchCone.MinRadius, range);
        float t = max == OverwatchCone.MinRadius ? 1f : Mathf.Clamp01((radius - OverwatchCone.MinRadius) / (float)(max - OverwatchCone.MinRadius));
        return Mathf.RoundToInt(Mathf.Lerp(0, MaxPenalty, t));
    }

    // The cone's reach is read off the locked zone itself (the farthest cell of PartOverwatch.OverwatchArea from
    // the shooter), not OverwatchCone.LockedRadius: that one is rewritten by every later GetOrientedPattern call
    // for the same caster (hover previews, UI refreshes), which after the cast dropped it to the 3-cell minimum,
    // a 0 penalty, and so stripped the debuff from everyone in the cone.
    public static int ReachOf(BaseUnitEntity shooter, PartOverwatch part)
    {
        if (shooter.CurrentUnwalkableNode is not CustomGridNodeBase from)
            return OverwatchCone.MinRadius;
        int reach = 0;
        foreach (var node in part.OverwatchArea)
            reach = Math.Max(reach, from.CellDistanceTo(node));
        return Math.Max(reach, OverwatchCone.MinRadius);
    }

    public static int PenaltyOf(BaseUnitEntity shooter, PartOverwatch part) =>
        PenaltyFor(ReachOf(shooter, part), part.Ability.RangeCells);

    public static bool IsActiveCone(PartOverwatch part) =>
        part != null && !part.IsStopped && OverwatchCone.IsOurs(part.Ability);

    public static BlueprintBuff Blueprint => ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(BuffGuid);
}

[HarmonyPatch(typeof(OverwatchController), nameof(OverwatchController.Tick))]
internal static class OverwatchDebuff_Tick
{
    private static void Postfix()
    {
        try
        {
            var blueprint = OverwatchDebuff.Blueprint;
            if (blueprint == null)
                return;

            // Highest penalty any active overwatcher currently has on each enemy in its cone.
            var best = new Dictionary<BaseUnitEntity, int>();
            if (TurnController.IsInTurnBasedCombat())
            {
                foreach (var caster in Game.Instance.State.AllBaseAwakeUnits)
                {
                    var part = caster.GetOptional<PartOverwatch>();
                    if (!OverwatchDebuff.IsActiveCone(part))
                        continue;
                    int penalty = OverwatchDebuff.PenaltyOf(caster, part);
                    foreach (var unit in Game.Instance.State.AllBaseAwakeUnits)
                    {
                        if (unit.LifeState.IsDeadOrUnconscious || !caster.IsEnemy(unit) || !part.Contains(unit))
                            continue;
                        if (!best.TryGetValue(unit, out var existing) || penalty > existing)
                            best[unit] = penalty;
                    }
                }
            }

            int cones = 0;
            foreach (var u in Game.Instance.State.AllBaseAwakeUnits)
                if (OverwatchDebuff.IsActiveCone(u.GetOptional<PartOverwatch>()))
                    cones++;
            if (cones > 0)
                Diag.Log("debuff", $"inCombat={TurnController.IsInTurnBasedCombat()} activeCones={cones} enemiesInCones={best.Count}");

            foreach (var unit in Game.Instance.State.AllBaseAwakeUnits)
            {
                var buff = unit.Buffs.GetBuff(blueprint);
                if (best.TryGetValue(unit, out var penalty))
                {
                    // Every enemy in the cone is marked; a rank can't be 0, so a minimum-reach cone (no penalty)
                    // shows rank 1.
                    int rank = Math.Max(1, penalty);
                    if (buff == null)
                        buff = unit.Buffs.Add(blueprint, unit);
                    if (buff == null)
                        continue;
                    if (buff.Rank < rank)
                        buff.AddRank(rank - buff.Rank);
                    else if (buff.Rank > rank)
                        buff.RemoveRank(buff.Rank - rank);
                }
                else
                {
                    buff?.Remove();
                }
            }
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: overwatched debuff");
        }
    }
}

// The actual accuracy hit: added to the real attack roll, under the Overwatch ability's name, exactly like Reflex
// Shot's own aim modifier. An overwatch shot's PartOverwatch is still active (non-stopped) when the roll happens
// (unlike Reflex Shot, Overwatch's Mode doesn't stop on the first hit), so the penalty is read straight from it.
[HarmonyPatch(typeof(RulePerformAttackRoll), nameof(RulePerformAttackRoll.OnTrigger))]
internal static class OverwatchDebuff_AimModifier
{
    private static void Prefix(RulePerformAttackRoll __instance)
    {
        try
        {
            if (__instance.Initiator is not BaseUnitEntity shooter)
                return;
            var part = shooter.GetOptional<PartOverwatch>();
            if (!OverwatchDebuff.IsActiveCone(part))
                return;
            int penalty = OverwatchDebuff.PenaltyOf(shooter, part);
            if (penalty > 0)
                __instance.HitChanceRule.HitChanceValueModifiers.Add(-penalty, part.Ability.Fact);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: overwatch aim penalty");
        }
    }
}
