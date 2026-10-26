using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using UnityEngine;

namespace RogueTraderOverwatch;

// Loads Icons/<name>.png from the mod folder onto our blueprints, so icons need no Unity asset bundle.
[HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.ApplyContent))]
internal static class Icons
{
    private static readonly Dictionary<string, string> Files = new()
    {
        ["ReflexShot"] = "27f7f769d0b94ab18fee0cde8a577a4d",
        ["Overwatch"] = "b5855687981e4ba9a7dfc8c4af03229f",
        ["OverwatchShots"] = "e5ce47452a6d47d78e88178f4ee7bba5",
        ["Suppression"] = "5db3949e7bf544bcb10cc3a2d51eb9ea",
        ["Suppressed"] = "c6af42a8c37040a49f86559e80959c5f",
    };

    private static readonly AccessTools.FieldRef<BlueprintMechanicEntityFact, Sprite> IconField =
        AccessTools.FieldRefAccess<BlueprintMechanicEntityFact, Sprite>("m_Icon");

    // ImageConversion's assembly targets netstandard 2.1, which net48 can't compile against, so it's called by reflection.
    private static readonly MethodInfo LoadImage =
        Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
            .GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

    // ApplyContent loads the mod's blueprints, after the entry point has run.
    private static void Postfix(OwlcatModification __instance)
    {
        if (__instance.Manifest?.UniqueName != "RogueTrader.Overwatch")
            return;
        foreach (var pair in Files)
        {
            string name = pair.Key;
            try
            {
                var path = Path.Combine(__instance.Path, "Icons", name + ".png");
                var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintMechanicEntityFact>(pair.Value);
                if (!File.Exists(path) || blueprint == null)
                    continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name };
                if (!(bool)LoadImage.Invoke(null, new object[] { texture, File.ReadAllBytes(path) }))
                    continue;
                IconField(blueprint) = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: loading icon " + name);
            }
        }
    }
}
