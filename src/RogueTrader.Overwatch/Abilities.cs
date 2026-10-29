using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Code.Enums.Helper;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace RogueTraderOverwatch;

// The three abilities, shared by heroes (their own facts, Weapon == null) and enemies (weapon slots).
internal static class Abilities
{
    public const string ReflexShot = "27f7f769d0b94ab18fee0cde8a577a4d";
    public const string Overwatch = "b5855687981e4ba9a7dfc8c4af03229f";
    public const string Suppression = "5db3949e7bf544bcb10cc3a2d51eb9ea";

    public static readonly string[] All = { ReflexShot, Overwatch, Suppression };

    public static bool IsOurs(BlueprintAbility blueprint) => blueprint != null && Array.IndexOf(All, blueprint.AssetGuid) >= 0;

    public static bool IsOurs(AbilityData ability) => IsOurs(ability?.Blueprint);

    // The first ranged weapon in the active set: main hand, off hand, then extra limbs. GetFirstWeapon would
    // return a melee main hand even when the off hand holds a pistol.
    public static ItemEntityWeapon RangedWeapon(MechanicEntity unit)
    {
        if (unit is not BaseUnitEntity owner || owner.Body == null)
            return null;
        var weapons = new List<ItemEntityWeapon> { owner.Body.PrimaryHand.MaybeWeapon, owner.Body.SecondaryHand.MaybeWeapon };
        weapons.AddRange(owner.Body.AdditionalLimbs.Select(l => l.MaybeWeapon));
        return weapons.FirstOrDefault(w => w != null && w.Blueprint.IsRanged);
    }

    // The ranged weapon's own attack: what every overwatch shot fires.
    public static AbilityData RangedAttack(MechanicEntity unit) =>
        RangedWeapon(unit)?.Abilities.FirstOrDefault()?.Data;
}

// A hero's copy has no weapon, so Range: Weapon would resolve to nothing; use the active ranged weapon's range.
[HarmonyPatch(typeof(RuleCalculateAbilityRange), nameof(RuleCalculateAbilityRange.OnTrigger))]
internal static class Abilities_RangeFromActiveWeapon
{
    private static readonly MethodInfo SetResult = AccessTools.PropertySetter(typeof(RuleCalculateAbilityRange), nameof(RuleCalculateAbilityRange.Result));

    private static void Postfix(RuleCalculateAbilityRange __instance)
    {
        try
        {
            var ability = __instance.Ability;
            if (!Abilities.IsOurs(ability) || ability.Weapon != null || ability.Blueprint.Range != AbilityRange.Weapon)
                return;
            var attack = Abilities.RangedAttack(ability.Caster);
            if (attack != null)
                SetResult.Invoke(__instance, new object[] { attack.RangeCells });
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: ability range");
        }
    }
}

// Weapon size costs more to commit to: a Heavy ranged weapon (WeaponHeaviness.Heavy, the same flag the base
// game's own AP-cost components key off for Heavy-only surcharges) costs 1 extra AP to cast Overwatch or Suppression
// (Reflex Shot stays 1 AP, RTO-T82),
// on top of the normal 1 AP, so a heavy weapon's wielder can't set up a cone or pin a target as cheaply as a
// pistol or basic weapon can. Same lookup as the range fix (Abilities.RangedWeapon), so heroes and enemies share
// the rule: an enemy's weapon-slot ability already carries its own weapon, a hero's reads the active set's.
[HarmonyPatch(typeof(RuleCalculateAbilityActionPointCost), nameof(RuleCalculateAbilityActionPointCost.OnTrigger))]
internal static class Abilities_ActionPointCostByWeaponSize
{
    private const int HeavyWeaponCostBonus = 1;

    private static void Prefix(RuleCalculateAbilityActionPointCost __instance)
    {
        try
        {
            var ability = __instance.AbilityData;
            // Reflex Shot is a quick reaction: 1 AP whatever the weapon.
            if (!Abilities.IsOurs(ability) || ability.Blueprint.AssetGuid == Abilities.ReflexShot)
                return;
            var weapon = ability.Weapon ?? Abilities.RangedWeapon(ability.Caster);
            if (weapon?.Blueprint.Heaviness == WeaponHeaviness.Heavy)
                __instance.CostBonus += HeavyWeaponCostBonus;
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: ability AP cost");
        }
    }
}

// The pattern delivery applies the ability's effects once per unit inside the area: an empty zone would never
// start overwatch, and several enemies would restart it once each. Deliver once, to the clicked target.
[HarmonyPatch(typeof(WarhammerAbilityAttackDelivery), nameof(WarhammerAbilityAttackDelivery.Deliver))]
internal static class Abilities_DeliverOnce
{
    private static bool Prefix(AbilityExecutionContext context, TargetWrapper target, ref IEnumerator<AbilityDeliveryTarget> __result)
    {
        try
        {
            if (!Abilities.IsOurs(context?.Ability))
                return true;
            __result = Once(target);
            return false;
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: delivering overwatch");
            return true;
        }
    }

    private static IEnumerator<AbilityDeliveryTarget> Once(TargetWrapper target)
    {
        yield return new AbilityDeliveryTarget(target);
    }
}

// The engine's own weapon-check crashes (and so greys the button out) for any caster with an empty additional
// limb: its loop reads additionalLimb.Weapon even when additionalLimb.HasWeapon is false, throwing a
// NullReferenceException instead of returning whatever Primary/SecondaryHand already decided (documented as an
// untested quirk in DESIGN.md; confirmed in game on Pasqal, who has mechadendrites). Scoped to our three
// abilities only: everything else in the game keeps using the original, unpatched check.
[HarmonyPatch(typeof(AbilityCasterHasWeaponWithRangeType), nameof(AbilityCasterHasWeaponWithRangeType.IsCasterRestrictionPassed))]
internal static class Abilities_FixEmptyLimbWeaponCheck
{
    private static bool HasBurstAbility(ItemEntityWeapon weapon) =>
        weapon != null && weapon.Abilities.Any(p =>
            p.Blueprint.GetComponent<WarhammerAbilityAttackDelivery>()?.Special == WarhammerAbilityAttackDelivery.SpecialType.Burst);

    private static bool Suitable(AbilityCasterHasWeaponWithRangeType check, ItemEntityWeapon weapon) =>
        weapon != null && check.RangeType.IsSuitableAttackType(weapon.Blueprint.AttackType)
        && (!check.MustHaveBurst || weapon.GetWeaponStats().ResultRateOfFire > 1);

    private static bool Prefix(AbilityCasterHasWeaponWithRangeType __instance, MechanicEntity caster, ref bool __result)
    {
        if (__instance.OnlyMainHand || !Abilities.IsOurs(__instance.OwnerBlueprint as BlueprintAbility))
            return true;
        try
        {
            var body = caster.GetBodyOptional();
            if (body == null)
            {
                __result = false;
                return false;
            }
            bool flag = (body.PrimaryHand.HasWeapon && (Suitable(__instance, body.PrimaryHand.Weapon) || HasBurstAbility(body.PrimaryHand.Weapon)))
                     || (body.SecondaryHand.HasWeapon && (Suitable(__instance, body.SecondaryHand.Weapon) || HasBurstAbility(body.SecondaryHand.Weapon)));
            foreach (var limb in body.AdditionalLimbs)
            {
                if (!limb.HasWeapon)
                    continue;
                flag = flag || (__instance.RangeType.IsSuitableWeapon(limb.Weapon) && (!__instance.MustHaveBurst || limb.Weapon.GetWeaponStats().ResultRateOfFire > 1)) || HasBurstAbility(limb.Weapon);
            }
            __result = flag;
            return false;
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: weapon-range caster check");
            return true;
        }
    }
}

// UseFirstWeaponAbility fires GetFirstWeapon's attack, which is the melee weapon when a sword is in the main hand
// and a pistol in the off hand. Restart the overwatch on the ranged weapon's attack, before the shot budget is set.
[HarmonyPatch(typeof(AbilityEffectOverwatch), nameof(AbilityEffectOverwatch.Apply))]
internal static class Abilities_ShootWithRangedWeapon
{
    [HarmonyPriority(Priority.First)]
    private static void Postfix(AbilityExecutionContext context, TargetWrapper target)
    {
        try
        {
            if (!Abilities.IsOurs(context?.Ability) || context.MaybeCaster is not BaseUnitEntity caster)
                return;
            var part = caster.GetOptional<PartOverwatch>();
            var attack = Abilities.RangedAttack(caster);
            if (part == null || part.IsStopped || attack == null || part.AbilityOnTrigger?.Weapon == attack.Weapon)
                return;
            part.Start(context.Ability, attack, target, part.Mode, part.HitsPerTarget);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: choosing the ranged weapon");
        }
    }
}
