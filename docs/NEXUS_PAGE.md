# Nexus Mods page: Rogue Trader Overwatch

Not published yet. Copy each field into the Nexus Mods upload form. The description is BBCode, which is
what Nexus's editor accepts in source mode.

## Before uploading

Nothing in this mod has been run in the game yet. Work through [TESTING.md](TESTING.md) (the full
in-game checklist with expected results). At minimum these must pass before uploading:

- [ ] A1–A2: the mod window lists "Overwatch" as enabled, and the logs have no `RogueTrader.Overwatch`
      errors after loading a save.
- [ ] B1, B3: every party member, including a newly recruited companion, has the three buttons on the
      ability bar (slots 8–10 of row 1) with the custom icons.
- [ ] C1–C11: the buttons are lit with a gun in the active weapon set and greyed out with melee only,
      two melee weapons or empty hands, updating on weapon-set swaps and after closing the inventory.
- [ ] D2, E1, E4, E6: Reflex Shot fires once; the Overwatch cone resizes with the cursor, fires at most
      one shot per AP and re-shoots an enemy only after it moves 2 cells.
- [ ] F2–F5: Suppression draws one shot per move, none for acting in place, with cosmetic misses that
      do nothing.
- [ ] G1–G2, E9: enemies still fight normally and overwatch when they have nothing to shoot; a save made
      mid-overwatch keeps its shots.

## Mod name

Rogue Trader Overwatch

## Version

1.0.0 (upload `dist/RogueTrader.Overwatch-1.0.0.zip`, built by `dotnet build src/RogueTrader.Overwatch -c Release`)

## Category

Gameplay (alternative: Combat)

## Summary (max 350 characters)

Adds XCOM-style overwatch to every gun, for your party and your enemies. Reflex Shot fires at the first enemy
that moves near you, Overwatch watches a cone you size with the cursor and fires once per AP you saved, and
Suppression pins one enemy in place. Built on the game's own unused overwatch engine.

## Tags

Combat, Gameplay, Overhaul, Tactical, Abilities, AI

## Requirements

- Warhammer 40,000: Rogue Trader (Steam)
- No other mods required. Owlcat's mod loader is built into the game.
- DLC: not required.

## AI disclosure

The mod's art (logo, header and ability icons) was made with an image-generation AI, and its code was
written with an AI model (Claude). Tick Nexus's "contains AI-generated content" option when uploading.

## Images

Upload these from `assets/` in the Images tab, in this order. Make the first one the primary image.

| File | Use |
|---|---|
| `assets/Logo.png` | Primary image (square, 1254×1254). |
| `assets/Header.png` | Page header (1983×793). Set it as the header image in the page's media settings. |

Add in-game screenshots of each ability once it's been tested: the Overwatch cone preview, a Reflex Shot
firing and a suppressed enemy.

## Full description (BBCode)

Laid out in the five sections of Nexus's description template. Paste it with "Import description" in the
editor's top right corner, which converts BBCode.

```
[size=5][b]Description[/b][/size]
Rogue Trader's combat has no way to hold a position: anyone you don't shoot on your turn gets a free walk up to your guns on theirs. Overwatch adds three ways to cover ground, on every gun, for your party and your enemies alike.

It doesn't invent a new system. Owlcat built a complete overwatch engine into Rogue Trader and never shipped a way to use it. This mod puts buttons on it, then adds the rules a cone needs to feel right.

[size=5][b]Main features[/b][/size]
[b]Three new abilities for every character who carries a gun[/b]. Every party member gets them on the ability bar, in the last three slots of the first row (or the next free slots on rows 2 and 3), so you see them every turn. They're greyed out whenever your active weapon set has no ranged weapon (melee only, two melee weapons or empty hands) and light up the moment it does, including when you swap weapon sets. Take one off the bar and it stays off.

[list]
[*][b]Reflex Shot[/b]: overwatch the way XCOM does it. Pick the direction you expect trouble from; until your next turn, take one shot at the first enemy that moves or acts within 10 cells of you, in any direction. The shot is easier dead ahead (+10 aim) and harder the further you have to turn, down to -30 if you have to spin all the way around. Ends your turn.
[*][b]Overwatch[/b]: aim a cone and size it with the cursor. Aim close for a short, wide cone that covers a doorway (90° at 3 cells); aim far for a long, narrow lane down to your weapon's range (20°). You get one shot for each Action Point you had left, so holding fire pays. Fire is spread across different enemies first, and an enemy you've already shot is only shot again after it moves at least 2 cells, so nobody takes a full volley one step at a time. Ends your turn.
[*][b]Suppression[/b]: pin one enemy down. It can stay put and fight, but every time it moves it takes a ranged attack of opportunity. During its turn your hero keeps up covering fire that always misses, to make the threat plain. Ends your turn.
[/list]

[b]Fits the rules:[/b]
[list]
[*]Each ability costs 1 AP and ends your turn. Every shot is your weapon's normal attack, with its normal hit chance, which already drops with distance. With a sword and a pistol, the pistol fires.
[*]Overwatch ends when your next turn starts, when you act or when combat ends.
[*]Enemies with guns have the same three abilities. They still fight normally, but when they finish moving with nothing to shoot they overwatch the way you'd expect, and they suppress targets hiding in full cover. They also avoid overwatched ground when they move.
[*]An active Overwatch shows a buff with the shots you have left. It survives saving and loading.
[/list]

[size=5][b]Installation instructions[/b][/size]
[list=1]
[*]Download the archive.
[*]Extract the [b]RogueTrader.Overwatch[/b] folder into:
[code]%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\[/code]
[*]Start the game, open Owlcat's mod window (its key is listed under Settings, Controls, General), enable [b]Overwatch[/b] and restart the game.
[/list]

[b]Uninstalling:[/b] disable the mod in the mod window and delete the [b]RogueTrader.Overwatch[/b] folder. Remove it outside combat, so no unit is in the middle of an overwatch.

[size=5][b]Requirements[/b][/size]
[list]
[*]Warhammer 40,000: Rogue Trader. Nothing else. Owlcat's mod loader is built into the game, and no DLC is required.
[*]Compatible with most mods. A mod that changes the same enemy base fact or the overwatch system itself may conflict.
[/list]

[size=5][b]Shout outs[/b][/size]
[list]
[*][b]Owlcat Games[/b] for Rogue Trader, for the overwatch engine hiding inside it, and for the modding template that made it possible to find.
[*]The design borrows from [b]XCOM 2[/b]'s overwatch and Kill Zone, and from the aimed cones of [b]Gears Tactics[/b] and [b]Zero Company[/b].
[*]Code written with an AI model (Claude); art made with image-generation AI.
[/list]

Made by MindAttic. Warhammer 40,000 is a trademark of Games Workshop. This is an unofficial fan mod.
```
