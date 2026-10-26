# RogueTrader.Overwatch

Overwatch for Warhammer 40,000: Rogue Trader: three abilities that work like built-in ones, using
the game's own overwatch engine, which ships in the game unused.

- **Party:** every party member gets Reflex Shot, Overwatch and Suppression on their ability bar,
  placed in slots 8–10 of the first row (or the last slots of rows 2 and 3 when those are taken).
  They're greyed out whenever the active weapon set has no gun (melee only, two melee weapons,
  unarmed) and light up as soon as it has one. Removing one from the bar keeps it off.
- **Enemies:** any enemy with a pistol, rifle or heavy weapon has the same three abilities and uses
  them when it has nothing to shoot or every target is in full cover.

| Ability | What it does |
|---|---|
| **Reflex Shot** | XCOM 2 overwatch: pick the direction you expect them from, then fire once at the first enemy that moves or acts within 10 cells of you, in any direction. Aim +10 straight ahead, down to -30 if you have to spin all the way around. |
| **Overwatch** | Drag a cone: moving the cursor farther makes it longer and narrower (down to 20° at weapon range), closer makes it shorter and wider (90° at 3 cells). One shot per AP you had before casting, spread across the enemies that move or act in it; an enemy already shot is shot again only after moving 2+ cells. |
| **Suppression** | Pin one enemy: it can stay put and fight, but every move draws a ranged attack of opportunity. Your hero keeps up visible fire that always misses to sell the threat. |

All three cost 1 AP, end your turn and last until your next turn. Shots use your weapon's normal
attack and hit chance, which drops with distance. The full rules and how each one maps onto the
engine are in [docs/DESIGN.md](docs/DESIGN.md).

**Testing:** [docs/TESTING.md](docs/TESTING.md) is the step-by-step in-game checklist, with the
expected result for every rule and equip situation and where to look in the logs.

## Build and install

```
dotnet build src/RogueTrader.Overwatch
```

Needs [RogueTrader.Shared](https://github.com/mindattic/RogueTrader.Shared) checked out next to this repo
(shared game paths, references, packaging and helpers). Compiles the mod's assembly against the game's DLLs, then writes the Owlcat modification
(`mod/` plus `Assemblies/RogueTrader.Overwatch.dll`) to
`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\RogueTrader.Overwatch`
and enables it in `OwlcatModificationManagerSettings.json`. No Unity needed. Restart the game to
load it; Owlcat's mod window (its key is under Settings, Controls, General) shows it. Override
`GameDir` or `GameDataDir` with `-p:Name=value`. `dotnet build src/RogueTrader.Overwatch -c Release`
also writes the upload, `dist/RogueTrader.Overwatch-<version>.zip`.

- `mod/Blueprints/*.jbp`: the mod's own blueprints. `*.jbp_patch` edits a game blueprint, and
  each patch is listed in `mod/OwlcatModificationSettings.json`.
- `mod/Localization/enGB.json`: strings, as `{"strings": {"<key>": {"Text": "..."}}}`.
- `assets/*.png`: icons, square PNGs named after what they're for (`ReflexShot`, `Overwatch`,
  `OverwatchShots`, `Suppression`, `Suppressed`). Replace a file and rebuild to change an icon.
- `src/RogueTrader.Overwatch`: C# for what blueprints can't express. The game calls the method
  marked `[OwlcatModificationEnterPoint]`.

## Status

In development; nothing is released yet, and nothing has been run in the game: the next step is
[docs/TESTING.md](docs/TESTING.md). The Nexus Mods page is drafted in
[docs/NEXUS_PAGE.md](docs/NEXUS_PAGE.md). Work is tracked as RTO tickets
(see [docs/USER_STORIES.md](docs/USER_STORIES.md)).
