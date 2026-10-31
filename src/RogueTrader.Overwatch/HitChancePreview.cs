using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Patterns;
using Kingmaker.Utility;
using UnityEngine;

namespace RogueTraderOverwatch;

// While the player aims Overwatch or Reflex Shot, the game's own hit-chance overtip above each enemy in the area (the
// round gauge and damage range a normal attack shows) carries the shot that enemy would take: the ranged weapon's
// attack, worked out by the game itself (range falloff, cover, dodge, talents, burst, damage), with our own effect on
// it applied the same way the real roll applies it (Overwatch's reach penalty, added; Reflex Shot's retained-aim
// fraction, multiplied, plus its 33% damage cut). Left alone, an area ability counts as "always hits" and the overtip
// shows just its icon. GatherAffectedTargetsData builds the per-target data the overtips read, each time the aimed
// area changes.
[HarmonyPatch(typeof(AbilityDataHelper), nameof(AbilityDataHelper.GatherAffectedTargetsData))]
internal static class HitChancePreview
{
    // Set only while one target's preview is being worked out: whose hit-chance rules get which effect, under which
    // fact's name, and whether it's a Reflex Shot (ReflexFraction, its own hit-chance and damage patches,
    // ReflexShot_BaseHitChance and ReflexShot_Damage) or an Overwatch cone (Modifier, additive).
    internal static (MechanicEntity Shooter, int Modifier, float ReflexFraction, EntityFact Source, bool Reflex)? Pending;

    private static void Postfix(AbilityData ability, OrientedPatternData pattern, Vector3 casterPosition, TargetWrapper clickedTarget, in List<AbilityTargetUIData> listToFill)
    {
        try
        {
            bool cone = OverwatchCone.IsOurs(ability);
            if (!cone && !ReflexShot.IsOurs(ability) || ability.Caster is not BaseUnitEntity aimer || listToFill == null)
                return;
            var attack = Abilities.RangedAttack(aimer);
            if (attack == null)
                return;
            int penalty = 0;
            if (cone)
            {
                // Same measure as the real roll (OverwatchDebuff.PenaltyOf): the farthest cell of the aimed cone.
                var cells = new List<CustomGridNodeBase>();
                foreach (var node in pattern.Nodes)
                    cells.Add(node);
                penalty = OverwatchDebuff.PenaltyFor(OverwatchDebuff.ReachOf(aimer, cells), attack.RangeCells);
            }
            // Reflex Shot's facing is the clicked point, which the overwatch keeps as its target (ReflexShot_RecordTurn).
            var facing = clickedTarget?.Point ?? aimer.Position + aimer.OrientationDirection;
            for (int i = 0; i < listToFill.Count; i++)
            {
                if (listToFill[i].Target is not BaseUnitEntity target || !aimer.IsEnemy(target))
                    continue;
                listToFill[i] = cone
                    ? ShotAt(ability, attack, aimer, target, casterPosition, -penalty, 1f)
                    : ShotAt(ability, attack, aimer, target, casterPosition, 0, ReflexShot.RetainedFractionFor(aimer, facing, target.Position));
            }
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: hit-chance preview");
        }
    }

    // The weapon attack's own preview data, under our ability (the overtip only shows data for the ability being aimed).
    private static AbilityTargetUIData ShotAt(AbilityData ability, AbilityData attack, BaseUnitEntity aimer, BaseUnitEntity target, Vector3 casterPosition, int modifier, float reflexFraction)
    {
        bool reflex = ReflexShot.IsOurs(ability);
        Pending = (aimer, modifier, reflexFraction, ability.Fact, reflex);
        // The damage rule cache (one frame) is keyed by attacker, target and weapon attack, not by our Reflex Shot
        // reduction: keep the reduced damage out of, and the full damage away from, everyone else's preview.
        if (reflex)
            RuleCache<CalculateDamageParams, RuleCalculateDamage>.ForceInvalidateCache();
        try
        {
            var overpenetration = new OverpenetrationUIData { CountOverpenetration = false, OverpenetrationDamagePercent = 100, OverpenetrationHitChance = 100f };
            return new AbilityTargetUIData(attack, target, casterPosition, ref overpenetration).CopyWithOverride(ability);
        }
        finally
        {
            Pending = null;
            if (reflex)
                RuleCache<CalculateDamageParams, RuleCalculateDamage>.ForceInvalidateCache();
        }
    }
}

// Overwatch's own additive reach penalty, for the preview only (Reflex Shot's retained-aim fraction is applied as a
// multiplier in ReflexShot_BaseHitChance instead, which runs for the preview roll the same as it does for a real one).
[HarmonyPatch(typeof(RuleCalculateHitChances), nameof(RuleCalculateHitChances.OnTrigger))]
internal static class HitChancePreview_AddModifier
{
    private static void Prefix(RuleCalculateHitChances __instance)
    {
        try
        {
            if (HitChancePreview.Pending is { } pending && pending.Modifier != 0 && pending.Source != null && __instance.Initiator == pending.Shooter)
                __instance.HitChanceValueModifiers.Add(pending.Modifier, pending.Source);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: hit-chance preview modifier");
        }
    }
}
