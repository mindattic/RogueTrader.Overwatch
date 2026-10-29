using System;
using Kingmaker;
using UnityEngine;

namespace RogueTraderOverwatch;

// Per-frame driver for the mod's world-space visuals (tile outlines, suppression lines, the
// suppressor's held pose). The simulation tick (OverwatchController.Tick) doesn't run every frame, e.g. while the
// player is aiming, so these update here instead, from Unity's own Update on a hidden object that lives for the
// whole session. Each part has its own try so one failing doesn't stop the others.
internal sealed class VisualsDriver : MonoBehaviour
{
    private static VisualsDriver s_Instance;
    private float m_NextErrorLog;

    public static void Ensure()
    {
        if (s_Instance != null)
            return;
        var go = new GameObject("RogueTraderOverwatch_Visuals");
        DontDestroyOnLoad(go);
        s_Instance = go.AddComponent<VisualsDriver>();
    }

    private void Update()
    {
        if (Game.Instance?.State == null)
            return;
        Run(AbilityOutline.Sync, "outline");
        Run(SuppressionLine.Sync, "suppression line");
        Run(Suppression.Instance.FreezePoses, "suppression pose");
    }

    private void Run(Action action, string what)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (Time.unscaledTime < m_NextErrorLog)
                return;
            m_NextErrorLog = Time.unscaledTime + 10f;
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: visuals (" + what + ")");
        }
    }
}
