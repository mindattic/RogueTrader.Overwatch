using System;
using Kingmaker;
using UnityEngine;

namespace RogueTraderOverwatch;

// Per-frame driver for the mod's world-space visuals (tile outlines, suppression lines). The simulation tick
// (OverwatchController.Tick) doesn't run every frame, e.g. while the player is aiming, so the visuals update here
// instead, from Unity's own Update on a hidden object that lives for the whole session.
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
        try
        {
            if (Game.Instance?.State == null)
                return;
            AbilityOutline.Sync();
            SuppressionLine.Sync();
        }
        catch (Exception ex)
        {
            if (Time.unscaledTime < m_NextErrorLog)
                return;
            m_NextErrorLog = Time.unscaledTime + 10f;
            PFLog.Mods.Exception(ex, "RogueTrader.Overwatch: visuals");
        }
    }
}
