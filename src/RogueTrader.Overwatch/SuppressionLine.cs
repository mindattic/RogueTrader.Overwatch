using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using UnityEngine;

namespace RogueTraderOverwatch;

// A glowing red line from each suppressor to the unit it has Suppressed, for as long as the debuff lasts: the
// suppressor's weapon is trained on the target. Only for a weapon whose attack has no projectile (e.g. Pasqal's
// staff); one that fires projectiles shows the suppression through its cosmetic near-miss fire instead. No asset: two LineRenderers per pair on Unity's built-in
// Hidden/Internal-Colored shader, a wide translucent red halo under a thin bright core, pulsing slowly. Kept in sync
// every tick from the Suppressed buffs themselves (caster = suppressor), so it appears and disappears with them.
internal static class SuppressionLine
{
    private const float CoreWidth = 0.03f;
    private const float HaloWidth = 0.14f;
    private const float PulseSpeed = 3f;
    private static readonly Color Core = new Color(1f, 0.15f, 0.1f, 1f);
    private static readonly Color Halo = new Color(1f, 0f, 0f, 0.35f);

    private sealed class Tether
    {
        public LineRenderer CoreLine;
        public LineRenderer HaloLine;
    }

    private static readonly Dictionary<(string Caster, string Target), Tether> Active = new();

    public static void Sync()
    {
        var live = new HashSet<(string, string)>();
        if (TurnController.IsInTurnBasedCombat())
        {
            foreach (var target in Game.Instance.State.AllBaseAwakeUnits)
            {
                if (target.LifeState.IsDeadOrUnconscious)
                    continue;
                foreach (var buff in target.Buffs.Enumerable)
                {
                    if (buff.Blueprint?.AssetGuid != Suppression.BuffGuid || buff.Context?.MaybeCaster is not BaseUnitEntity caster)
                        continue;
                    // A weapon with a projectile shows its suppression through the cosmetic near-miss fire; the line
                    // is for weapons with nothing to fly (RTO-T84).
                    if (caster.LifeState.IsDeadOrUnconscious || Suppression.FiresProjectiles(caster))
                        continue;
                    var key = (caster.UniqueId, target.UniqueId);
                    live.Add(key);
                    if (!Active.TryGetValue(key, out var tether) || tether.CoreLine == null)
                        Active[key] = tether = Create();
                    Place(tether, caster, target);
                }
            }
        }

        var stale = new List<(string, string)>();
        foreach (var pair in Active)
            if (!live.Contains(pair.Key))
                stale.Add(pair.Key);
        foreach (var key in stale)
        {
            var tether = Active[key];
            Active.Remove(key);
            if (tether.CoreLine != null)
                UnityEngine.Object.Destroy(tether.CoreLine.gameObject);
            if (tether.HaloLine != null)
                UnityEngine.Object.Destroy(tether.HaloLine.gameObject);
        }
    }

    private static void Place(Tether tether, BaseUnitEntity caster, BaseUnitEntity target)
    {
        var from = caster.EyePosition;
        var to = target.Position + Vector3.up * 1.2f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * PulseSpeed);
        foreach (var (line, color) in new[] { (tether.CoreLine, Core), (tether.HaloLine, Halo) })
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            var c = new Color(color.r, color.g, color.b, color.a * pulse);
            line.startColor = c;
            line.endColor = c;
        }
    }

    private static Tether Create() => new Tether
    {
        HaloLine = Line("RogueTraderOverwatch_SuppressionHalo", HaloWidth, 3999),
        CoreLine = Line("RogueTraderOverwatch_SuppressionCore", CoreWidth, 4000),
    };

    private static LineRenderer Line(string name, float width, int queue)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.loop = false;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.sharedMaterial = SharedMaterial(queue);
        return line;
    }

    // One additive material per render queue, shared by every tether (a material per line would never be freed).
    private static readonly Dictionary<int, Material> Materials = new();

    private static Material SharedMaterial(int queue)
    {
        if (Materials.TryGetValue(queue, out var material) && material != null)
            return material;
        var shader = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        material = new Material(shader) { renderQueue = queue };
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive: reads as a glow
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        UnityEngine.Object.DontDestroyOnLoad(material);
        Materials[queue] = material;
        return material;
    }
}
