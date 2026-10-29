using System.Collections.Generic;
using Kingmaker;
using UnityEngine;

namespace RogueTraderOverwatch;

// Temporary in-game diagnostics (RTO-T59/T61): throttled log lines tagged "RogueTrader.Overwatch: diag" so a single
// test run shows whether the tick and preview hooks fire and what they compute. Remove once both are confirmed.
internal static class Diag
{
    private const float Interval = 2f;
    private static readonly Dictionary<string, float> Last = new();

    public static void Log(string key, string message)
    {
        if (Last.TryGetValue(key, out var at) && Time.time - at < Interval)
            return;
        Last[key] = Time.time;
        PFLog.Mods.Log("RogueTrader.Overwatch: diag " + key + ": " + message);
    }
}
