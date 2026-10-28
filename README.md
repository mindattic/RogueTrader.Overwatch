# RogueTrader.Overwatch

Overwatch for Warhammer 40,000: Rogue Trader: three abilities that let any gun hold ground, built on
the game's own overwatch engine (Owlcat shipped it in the game but never gave players a way to use it).

| Ability | What it does |
|---|---|
| **Reflex Shot** | XCOM-style overwatch. Pick the direction you expect them from; until your next turn, take one shot at the first enemy that moves (including being pushed) or acts within 10 cells, in any direction. Aim +10 straight ahead, falling to −30 when you have to spin all the way around. |
| **Overwatch** | Aim a cone sized by the cursor: close gives a short, wide cone (3 cells, 90°), far a long, narrow one (up to weapon range, 20°). One shot per AP you had before casting, at enemies that move, are pushed or act inside it. Fire spreads across enemies first; one already shot is shot again only after it moves 2+ cells. A buff shows the shots left and survives save and load. |
| **Suppression** | Pin one enemy. It can stay put and fight, but every time it leaves its square, by walking, charging, jumping or being pushed, it takes a ranged attack of opportunity (one per move or ability, not per square). During its turn your hero keeps up covering fire that always misses. |

All three cost 1 AP, end your turn and last until your next turn. Every shot is your weapon's normal
attack with its normal hit chance, which already drops with distance.

**Who gets them**
- **Party:** every party member, on their ability bar in slots 8–10 of the first row (or the last
  free slots of rows 2 and 3). Greyed out whenever the active weapon set has no gun (melee only, two
  melee weapons, unarmed); lit as soon as it has one. A button you remove stays removed.
- **Enemies:** any enemy holding a pistol, rifle or heavy weapon. They fight normally, overwatch when
  they've finished moving with nothing to shoot and a party member is within weapon range + 8 cells
  (Overwatch with 2+ AP, Reflex Shot with 1), and suppress when every target they can shoot is in full
  cover.
- **Pets:** never, even with a ranged weapon: they don't plan ahead.

The full rules and how each maps onto the engine: [docs/DESIGN.md](docs/DESIGN.md).

## Languages

Ability and buff names and descriptions are translated into all 8 of the game's official UI
languages, matching whichever one the game itself is set to:

- English (`enGB`)
- French (`frFR`)
- German (`deDE`)
- Spanish - Spain (`esES`)
- Russian (`ruRU`)
- Simplified Chinese (`zhCN`)
- Japanese (`jaJP`)
- Turkish (`trTR`)

Audio stays English-only, since that's all Rogue Trader itself ships.

## Build and install

Needs [RogueTrader.Shared](https://github.com/mindattic/RogueTrader.Shared) checked out next to this repo.

```
dotnet build src/RogueTrader.Overwatch
```

Compiles the mod against the game's DLLs (no Unity), installs it to
`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\RogueTrader.Overwatch`
and enables it; start the game. `-c Release` also writes the upload,
`dist/RogueTrader.Overwatch-<version>.zip`. Override `GameDir` or `GameDataDir` with `-p:Name=value`.

The installed folder must keep its empty `Bundles` subfolder: the game's loader lists it without
checking it exists, and without it no blueprint in the mod loads.

| Path | Holds |
|---|---|
| `mod/Blueprints/` | The mod's blueprints (`*.jbp`) and patches to game blueprints (`*.jbp_patch`, listed in `mod/OwlcatModificationSettings.json`) |
| `mod/Localization/*.json` | Strings per UI language, as `{"strings": {"<key>": {"Text": "..."}}}`; one file per locale (`enGB`, `frFR`, `deDE`, `esES`, `ruRU`, `zhCN`, `jaJP`, `trTR`) |
| `assets/` | Hotbar icons (`ReflexShot`, `Overwatch`, `OverwatchShots`, `Suppression`, `Suppressed`; square PNGs, replace and rebuild) and the Nexus `Logo.png` and `Header.png` |
| `src/RogueTrader.Overwatch/` | C# (Harmony patches) for what blueprints can't express |
| `docs/` | [DESIGN.md](docs/DESIGN.md), [TESTING.md](docs/TESTING.md), [NEXUS_PAGE.md](docs/NEXUS_PAGE.md), the ticket log |

## Testing

[docs/TESTING.md](docs/TESTING.md) is the in-game checklist: every rule and equip situation, its
expected result, and what to look for in `Player.log`.

## Status

**2.0.0: ready for in-game testing, not released.** Adds Localization/{frFR,deDE,esES,ruRU,zhCN,jaJP,trTR}.json,
covering all 8 of the game's official UI languages. Work through TESTING.md before uploading
`dist/RogueTrader.Overwatch-2.0.0.zip` with [docs/NEXUS_PAGE.md](docs/NEXUS_PAGE.md).

Versions go up a whole number each release (1.0.0, 2.0.0, 3.0.0): bump `Version` in
`mod/OwlcatModificationManifest.json`, which names the zip.
