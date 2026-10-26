using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
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
