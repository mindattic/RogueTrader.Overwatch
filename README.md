# RogueTrader.Overwatch

Overwatch for Warhammer 40,000: Rogue Trader. Every hero and enemy holding a ranged weapon gets
Overwatch buttons on that weapon's action bar, working like built-in abilities. They use the
game's own overwatch engine, which ships in the game unused.

| Ability | What it does |
|---|---|
| **Reaction Shot** | XCOM 2 overwatch: fire once at the first enemy that moves or acts within range around you, then it ends. |
| **Overwatch** | Drag a cone: moving the cursor farther makes it longer and narrower, closer makes it shorter and wider. One shot per AP you had left, spread across the enemies that move or act in it. |
| **Suppression** | Pin one enemy: it can stay put and fight, but every move draws a ranged attack of opportunity. Your hero keeps up visible fire that always misses to sell the threat. |

All three end your turn. Shots use your weapon's normal attack and hit chance, which drops with
distance. The full rules and how each one maps onto the engine are in
[docs/DESIGN.md](docs/DESIGN.md).

## Build and install

```
dotnet build src/RogueTrader.Overwatch
```

Compiles the mod's assembly against the game's DLLs, then writes the Owlcat modification
(`mod/` plus `Assemblies/RogueTrader.Overwatch.dll`) to
`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\RogueTrader.Overwatch`
and enables it in `OwlcatModificationManagerSettings.json`. No Unity needed. Restart the game to
load it; Shift+F10 opens Owlcat's mod manager. Override `GameDir` or `GameDataDir` with
`-p:Name=value`.

- `mod/Blueprints/*.jbp`: the mod's own blueprints. `*.jbp_patch` edits a game blueprint, and
  each patch is listed in `mod/OwlcatModificationSettings.json`.
- `mod/Localization/enGB.json`: strings, as `{"strings": {"<key>": {"Text": "..."}}}`.
- `src/RogueTrader.Overwatch`: C# for what blueprints can't express. The game calls the method
  marked `[OwlcatModificationEnterPoint]`.

## Status

In development; nothing is released yet. Work is tracked as RTO tickets
(see [docs/USER_STORIES.md](docs/USER_STORIES.md)).
