# Nexus Mods page: Rogue Trader Overwatch

Not published yet (live link goes here once uploaded). Copy each field into the Nexus Mods upload form.
The description is BBCode, which is what Nexus's editor accepts in source mode.

## Before uploading

Nothing in this mod has been run in the game yet. Work through [TESTING.md](TESTING.md), the full in-game
checklist with expected results. At minimum these must pass:

- [ ] A1–A2: the mod window lists "Overwatch" as enabled, and `Player.log` has no `RogueTrader.Overwatch`
      errors after loading a save.
- [ ] B1, B3, B4a: every party member, including a newly recruited companion, has the three buttons in
      slots 8–10 with the custom icons; pets have none.
- [ ] C1–C11: the buttons are lit with a gun in the active weapon set and greyed out with melee only,
      two melee weapons or empty hands, updating on weapon-set swaps and after closing the inventory.
- [ ] D1–D2c: Reflex Shot faces where you click, fires once, shows the "Reflex Shot" aim modifier, and
      fires at an enemy pushed into range.
- [ ] E1, E4, E6, E6a, E9: the Overwatch cone resizes with the cursor, fires at most one shot per AP,
      re-shoots only after 2 cells of movement (pushes count), and keeps its shots across a save and load.
- [ ] F2–F5: Suppression fires once per move, charge, jump or push off its square, never for acting in
      place, with cosmetic misses that change nothing.
- [ ] G1–G2: enemies still fight normally and overwatch when they have nothing to shoot.

## Mod name

Rogue Trader Overwatch

## Version

1.0.0 (upload `dist/RogueTrader.Overwatch-1.0.0.zip`, built by `dotnet build src/RogueTrader.Overwatch -c Release`)

## Changelog

- **1.0.0:** First release.

## Category

Gameplay (alternative: Combat)

## Summary (max 350 characters)

Overwatch for every gun in Rogue Trader, for your party and enemies. Reflex Shot and Suppression play like XCOM: fire at the first enemy to move near you, or pin a foe so leaving its square gets it shot. Overwatch is a Gears Tactics / Star Wars Zero Company cone you size with the cursor, one shot per AP saved. Greys out without a gun.

## Tags

Combat, Gameplay, Tactical, Abilities, AI, Overhaul

## Requirements

- Warhammer 40,000: Rogue Trader (Steam)
- No other mods required. Owlcat's mod loader is built into the game.
- DLC: not required.

## AI disclosure

The mod's art (logo, header and ability icons) was made with an image-generation AI, and its code was
written with an AI model (Claude). Tick Nexus's "contains AI-generated content" option when uploading.

## Images

Upload these from `assets/` in the Images tab, in this order. Make the first one the primary image.

| File | Use | Caption |
|---|---|---|
| `assets/Logo.png` | Primary image (square, 1254×1254) | Rogue Trader Overwatch |
| `assets/Header.png` | Page header (1983×793): set it as the header image in the page's media settings | Hold the line |

In-game screenshots get added after testing: the Overwatch cone preview, a Reflex Shot firing with its aim
modifier, and a suppressed enemy under covering fire. Once uploaded, right-click each one on the mod page,
copy its image address, and paste it over `IMAGE_1_URL`, `IMAGE_2_URL` and `IMAGE_3_URL` in the description
below.

## Full description (BBCode)

Laid out in the five sections of Nexus's description template. Paste it with "Import description" in the
editor's top right corner, which converts BBCode.

```
[size=5][b]Description[/b][/size]
Rogue Trader's combat has no way to hold a position: anyone you don't shoot on your turn gets a free walk up to your guns on theirs. Overwatch gives every gun three ways to cover ground, for your party and your enemies alike.

It doesn't invent a new system. Owlcat built a complete overwatch engine into Rogue Trader and never shipped a way to use it. This mod puts buttons on it, then adds the rules a cone needs to feel right.

[b]Overwatch: the cone sized by your cursor[/b]
[img]IMAGE_1_URL[/img]

[b]Reflex Shot: aim modifier for the turn it takes[/b]
[img]IMAGE_2_URL[/img]

[b]Suppression: covering fire on a pinned enemy[/b]
[img]IMAGE_3_URL[/img]

[size=5][b]Installation instructions[/b][/size]
[list=1]
[*]Download the archive.
[*]Extract the [b]RogueTrader.Overwatch[/b] folder into:
[code]%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\[/code]
Keep everything inside it, including the empty [b]Bundles[/b] folder: the game won't load the mod without it.
[*]Start the game, open Owlcat's mod window (its key is listed under Settings, Controls, General), enable [b]Overwatch[/b] and restart the game.
[/list]

[b]Uninstalling:[/b] disable the mod in the mod window and delete the [b]RogueTrader.Overwatch[/b] folder. Remove it outside combat, so no unit is in the middle of an overwatch.

[size=5][b]Main features[/b][/size]
[b]Three new abilities on every gun.[/b] Every party member gets them on the ability bar, in the last three slots of the first row (or the next free slots on rows 2 and 3). They're greyed out whenever the active weapon set has no gun (melee only, two melee weapons or unarmed) and light up as soon as it has one. Remove one from the bar and it stays removed.

[list]
[*][b]Reflex Shot[/b]: overwatch the way XCOM does it. Click the direction you expect trouble from. Until your next turn, take one shot at the first enemy that moves or acts within 10 cells of you, in any direction. Aim gets +10 when the enemy comes from straight ahead, falling to -30 when you have to spin all the way around to fire; the modifier shows in the hit-chance breakdown as "Reflex Shot".
[*][b]Overwatch[/b]: aim a cone and size it with the cursor. Aim close for a short, wide cone that covers a doorway; aim far for a long, narrow lane out to your weapon's range. You get one shot for each Action Point you had left, so holding fire pays. Fire is spread across different enemies first, and an enemy you've already shot is only shot again after it moves at least 2 cells, so nobody takes a full volley one step at a time. A buff shows the shots you have left.
[*][b]Suppression[/b]: pin one enemy down. It can stay put and fight, but every time it leaves its square, by walking, charging, jumping or being pushed, it takes a ranged attack of opportunity (one per move, not one per square). During its turn your hero keeps up covering fire that always misses, to make the threat plain.
[/list]

[b]Fits the rules:[/b]
[list]
[*]Each ability costs 1 AP, ends your turn and lasts until your next turn.
[*]Every shot is your weapon's normal attack, with its normal hit chance, which already drops with distance.
[*]Being pushed counts as moving: shove an enemy into a Reflex Shot circle or an Overwatch cone and it draws fire.
[*]Overwatch ends when your next turn starts, when you act or when combat ends. An active Overwatch survives saving and loading.
[*]Enemies with pistols, rifles and heavy weapons have the same three abilities. They still fight normally, but when they finish moving with nothing to shoot they overwatch you, and they suppress targets hiding in full cover. Enemies already steer around overwatched ground.
[*]Pets never overwatch, even with a ranged weapon: they don't plan ahead.
[/list]

[size=5][b]Requirements[/b][/size]
[list]
[*]Warhammer 40,000: Rogue Trader. Nothing else. Owlcat's mod loader is built into the game, and no DLC is required.
[*]Compatible with most mods. A mod that changes the enemies' shared base fact, the overwatch system itself or the ability bar's auto-fill may conflict.
[/list]

[size=5][b]Shout outs[/b][/size]
[list]
[*][b]Owlcat Games[/b] for Rogue Trader, for the overwatch engine hiding inside it, and for the modding template that made it possible to find.
[*]The design borrows from [b]XCOM 2[/b]'s overwatch and suppression, and from the cursor-sized overwatch cones of [b]Gears Tactics[/b] and [b]Star Wars Zero Company[/b].
[*]Code written with an AI model (Claude); art made with image-generation AI.
[/list]

Made by MindAttic. Warhammer 40,000 is a trademark of Games Workshop. This is an unofficial fan mod.
```
