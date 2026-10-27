using System;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Modding;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Progression.Features;

namespace RogueTraderOverwatch;

public static class Main
{
    // Enemies: weapon-slot abilities (through CommonMobFact). Party: the abilities as their own facts.
    public const string FeatureGuid = "4fdd0480cc514cd1bac75e806175fbb3";
    public const string PartyFeatureGuid = "f09f0c2891084ad2abc5bd8113a0eb80";

    [OwlcatModificationEnterPoint]
    public static void Load(OwlcatModification modification)
    {
        RogueTrader.Shared.ModLog.Name = "RogueTrader.Overwatch";
        new Harmony("RogueTrader.Overwatch").PatchAll(typeof(Main).Assembly);
        EventBus.Subscribe(new PartyGrant());
        EventBus.Subscribe(Suppression.Instance);
    }
}

// The party has no shared fact like CommonMobFact, so its feature is added here. Owning the abilities (instead of
// borrowing them from the weapon) puts them on the ability bar, greyed out without a ranged weapon.
internal sealed class PartyGrant : IAreaHandler, ICompanionChangeHandler, IPartyHandler
{
    public void OnAreaDidLoad()
    {
        foreach (var unit in Game.Instance.Player.AllCharacters)
            Grant(unit);
    }

    public void OnAreaBeginUnloading() { }

    public void HandleRecruit() => Grant(EventInvokerExtensions.BaseUnitEntity);

    public void HandleUnrecruit() { }

    // Recruiting a companion or hiring a mercenary goes through Player.AddCompanion, which raises these, not HandleRecruit.
    public void HandleAddCompanion() => Grant(EventInvokerExtensions.BaseUnitEntity);

    public void HandleCompanionActivated() => Grant(EventInvokerExtensions.BaseUnitEntity);

    public void HandleCompanionRemoved(bool stayInGame) { }

    public void HandleCapitalModeChanged() { }

    private static void Grant(BaseUnitEntity unit)
    {
        try
        {
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Main.PartyFeatureGuid);
            // Pets are in AllCharacters too, but never hold a gun.
            if (unit == null || unit.IsPet || feature == null)
                return;
            // Earlier builds gave the party the weapon-slot feature; drop it so the buttons don't appear twice.
            var weaponFeature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Main.FeatureGuid);
            if (weaponFeature != null && unit.Facts.Contains(weaponFeature))
                unit.Facts.Remove(weaponFeature);
            if (!unit.Facts.Contains(feature))
                unit.AddFact(feature);
            ActionBar.Place(unit);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: granting the party's abilities");
        }
    }
}
