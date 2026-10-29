using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Parts;

namespace RogueTraderOverwatch;

// Taking any real damage breaks a unit's concentration: its Overwatch or Reflex Shot stops, and every Suppressed it
// put on someone is removed (which also clears the suppression line and cosmetic fire). Fake damage (previews) and
// zero-damage hits don't count.
[HarmonyPatch(typeof(RuleDealDamage), nameof(RuleDealDamage.OnTrigger))]
internal static class DamageInterrupt
{
    private static void Postfix(RuleDealDamage __instance)
    {
        try
        {
            if (__instance.IsFake || __instance.Result <= 0 || __instance.Target is not BaseUnitEntity victim)
                return;

            var part = victim.GetOptional<PartOverwatch>();
            if (part != null && !part.IsStopped && (OverwatchCone.IsOurs(part.Ability) || ReflexShot.IsOurs(part.Ability)))
                part.Stop();

            foreach (var unit in Game.Instance.State.AllBaseAwakeUnits)
                foreach (var buff in unit.Buffs.Enumerable.ToList())
                    if (buff.Blueprint?.AssetGuid == Suppression.BuffGuid && buff.Context?.MaybeCaster == victim)
                        buff.Remove();
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: damage interrupt");
        }
    }
}
