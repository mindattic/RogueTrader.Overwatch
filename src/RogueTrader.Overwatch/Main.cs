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
    public const string FeatureGuid = "4fdd0480cc514cd1bac75e806175fbb3";

    [OwlcatModificationEnterPoint]
    public static void Load(OwlcatModification modification)
    {
        EventBus.Subscribe(new PartyGrant());
    }
}

// Enemies get the Overwatch feature through the CommonMobFact patch; the party has no shared fact, so it's added here.
internal sealed class PartyGrant : IAreaHandler, ICompanionChangeHandler
{
    public void OnAreaDidLoad()
    {
        foreach (var unit in Game.Instance.Player.AllCharacters)
            Grant(unit);
    }

    public void OnAreaBeginUnloading() { }

    public void HandleRecruit() => Grant(EventInvokerExtensions.BaseUnitEntity);

    public void HandleUnrecruit() { }

    private static void Grant(BaseUnitEntity unit)
    {
        var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(Main.FeatureGuid);
        if (unit == null || feature == null || unit.Facts.Contains(feature))
            return;
        unit.AddFact(feature);
    }
}
