# Nexus Mods page: Rogue Trader Overwatch

Not published yet (live link goes here once uploaded). Copy each field into the Nexus Mods upload form.
The description is BBCode, which is what Nexus's editor accepts in source mode.

## Before uploading

3.0.0 has been played in game during development (outlines, suppression line and Suppressed debuff seen
working), but the full checklist hasn't been run. Work through [TESTING.md](TESTING.md), the full in-game
checklist with expected results. At minimum these must pass:

- [ ] A1–A2: the mod window lists "Overwatch" as enabled, and `Player.log` has no `RogueTrader.Overwatch`
      errors after loading a save.
- [ ] B1, B3, B4a: every party member, including a newly recruited companion, has the three buttons in
      slots 8–10 with the custom icons; pets have none.
- [ ] C1–C11: the buttons are lit with a gun in the active weapon set and greyed out with melee only,
      two melee weapons or empty hands, updating on weapon-set swaps and after closing the inventory.
- [ ] D1–D2e: Reflex Shot faces where you click, fires once, shows the "Reflex Shot" aim modifier, fires
      at an enemy pushed into range, and deals about 33% damage on every round of a burst.
- [ ] E1, E4–E4b, E6, E6a, E9: the Overwatch cone resizes with the cursor, fires at most 2 shots (1 with 1 AP),
      re-shoots only after 2 cells of movement (pushes count), and keeps its shots across a save and load.
- [ ] F2–F5a: Suppression fires once per move, charge, jump or push off its square, never for acting in
      place, with no-hit cosmetic fire and a held firing pose that change nothing, ending at once if the
      suppressor takes damage.
- [ ] G1–G2: enemies still fight normally and overwatch when they have nothing to shoot.
- [ ] J1–J2: a Heavy weapon makes Overwatch and Suppression cost 2 AP instead of 1; Reflex Shot stays 1 AP,
      and a Pistol or Basic weapon still costs 1 for all three.
- [ ] K1–K3: Suppression's and Overwatch's shots pick up bonuses from a talent/trait that specifically
      boosts attacks of opportunity; Reflex Shot's don't.
- [ ] N1–N4: a Suppressed unit's ranged hit chance, shown in the gauge and rolled, is 75% of its
      unsuppressed value; melee and scatter attacks are unchanged.
- [ ] E4c: when Overwatch fires with 2 shots, the second goes at once to the next-closest enemy in the cone.
- [ ] G9: a suppressed enemy mostly stays on its square and shoots back.
- [ ] G6, G8: enemies walk into Reflex Shot circles and Overwatch cones instead of routing around them; an enemy with a sword and an off-hand pistol overwatches with the pistol.
- [ ] C9a: a character with mechadendrites and a ranged weapon in a hand lights up with no log errors.
- [ ] L1–L5: the Overwatch snap-shot penalty scales -1 to -15 with the cone's reach, shows in the
      hit-chance breakdown, and the Overwatched debuff (rank = penalty) tracks the cone live and ends
      with the overwatch.
- [ ] D4a–D4b: Reflex Shot's circle reaches exactly the weapon's range, and its shot starts from a flat
      33 base hit chance (not Ballistic Skill + 30) plus the facing modifier.
- [ ] F5a: taking damage ends the unit's Suppression, Overwatch or Reflex Shot at once.
- [ ] M1–M4: the tile-edge outline (yellow for Reflex Shot, blue for Overwatch, 50% transparent) shows while aiming and while active, joins across height
      changes, clears on cancel, and every enemy inside the aimed area shows the game's own hit-chance gauge for that shot.

## Mod name

Rogue Trader Overwatch

## Version

3.0.0 (upload `dist/RogueTrader.Overwatch-3.0.0.zip`, built by `dotnet build src/RogueTrader.Overwatch -c Release`)

## Changelog

- **3.0.0:**
  - Heavy weapons: Overwatch and Suppression cost 2 AP instead of 1. Reflex Shot always costs 1 AP.
  - Reflex Shot: the circle now reaches your weapon's own range instead of a fixed 10 cells. It's a
    reaction, not an aimed shot: every shot hits from a flat 33% base instead of your Ballistic Skill,
    plus the facing modifier (+10 dead ahead down to -30 behind), and deals 33% of the weapon's damage.
  - Overwatch: at most 2 shots, and never more than the attacks your AP could have paid for (one per AP
    before, uncapped). Shots take a snap-shot penalty from -1 at the shortest reach to -15 at full weapon range.
    Every enemy standing in the cone shows it with a new Overwatched debuff. Its shots now count as
    attacks of opportunity, so talents and traits that boost attacks of opportunity apply to them. When it
    fires, the second shot goes at once to the next-closest enemy in the cone.
  - Suppression: its shot is now a real ranged attack of opportunity, so talents and traits that boost
    attacks of opportunity apply to it. A Suppressed unit's own ranged attacks keep 75% of their hit chance, and a suppressed enemy mostly stays put and shoots back. With a weapon that has no projectile (e.g. Pasqal's staff), a glowing red line links the suppressor to the target (RTO-T84); either way the
    suppressor holds a firing pose and keeps firing past the target (no hits, no damage, no ammo) for as
    long as the suppression lasts.
  - Taking damage ends a unit's Suppression, Overwatch or Reflex Shot at once.
  - Enemies walk into Reflex Shot circles and Overwatch cones as if they weren't there, so they get caught
    by the shots, and use an off-hand ranged
    weapon when the main hand holds a melee one.
  - A thin, 50%-transparent outline (yellow for Reflex Shot, blue for Overwatch) traces the tile edges of the Reflex Shot circle and the Overwatch cone while
    you aim and while they're active, and every enemy inside the area you're aiming shows the game's own
    hit-chance gauge and damage range for that shot.
  - Fixed: characters with mechadendrites (e.g. Pasqal) had all three buttons greyed out.
  - All of the above in all 8 languages.
- **2.0.0:** Full localization for all 8 of the game's official UI languages: French, German, Spanish,
  Russian, Simplified Chinese, Japanese and Turkish join the existing English text.
- **1.0.0:** First release.

## Category

Gameplay (alternative: Combat)

## Summary (max 350 characters)

Overwatch for every gun in Rogue Trader, for your party and enemies. Reflex Shot and Suppression play like XCOM: fire at the first enemy to move near you, or pin a foe so leaving its square gets it shot. Overwatch is a Gears Tactics / Star Wars Zero Company cone you size with the cursor, up to two shots. Greys out without a gun.

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
[*][b]Reflex Shot[/b]: overwatch the way XCOM does it. Click the direction you expect trouble from. Until your next turn, take one shot at the first enemy that moves or acts within your weapon's range, in any direction. It's a reaction, not an aimed shot: it hits from a flat 33% base instead of your Ballistic Skill, plus +10 when the enemy comes from straight ahead, falling to -30 when you have to spin all the way around to fire (the modifier shows in the hit-chance breakdown as "Reflex Shot"), and deals 33% of your weapon's damage. Always just 1 AP, whatever the gun. Enemies don't give its circle a wide berth: they walk in and risk the shot.
[*][b]Overwatch[/b]: aim a cone and size it with the cursor. Aim close for a short, wide cone that covers a doorway; aim far for a long, narrow lane out to your weapon's range. You get up to two shots: one for each attack the Action Points you had left would have paid for, never more than two, so it covers a lane without clearing a whole wave. When it fires, the first shot goes at the enemy caught moving and the second at once at the next-closest enemy in the cone; an enemy you've already shot is only shot again after it moves at least 2 cells, so nobody takes a full volley one step at a time. A buff shows the shots you have left. The farther the cone reaches, the harder the snap shot: an extra hit-chance penalty grows from -1 at close range up to -15 at full weapon range, and everyone currently standing in the cone shows it with an Overwatched debuff. It's a prepared ambush: every Overwatch shot counts as an attack of opportunity, so talents that boost attacks of opportunity apply.
[*][b]Suppression[/b]: pin one enemy down. It can stay put and fight, but every time it leaves its square, by walking, charging, jumping or being pushed, it takes a ranged attack of opportunity (one per move, not one per square). While Suppressed, its own ranged attacks lose a quarter of their hit chance, and enemies know it: a suppressed enemy would rather stay put and shoot back than move. For the whole suppression your hero holds a firing pose trained on it and keeps firing at it, a visual cue that never hits; a weapon with no projectile (like Pasqal's staff) shows a glowing red line between them instead.
[/list]

[b]Localization:[/b]
Ability and buff text is translated into all 8 of the game's official UI languages:
[list]
[*]English
[*]French
[*]German
[*]Spanish
[*]Russian
[*]Simplified Chinese
[*]Japanese
[*]Turkish
[/list]

[b]Fits the rules:[/b]
[list]
[*]Each ability costs 1 AP (Overwatch and Suppression 2 with a Heavy weapon active), ends your turn and lasts until your next turn.
[*]Every shot is your weapon's normal attack, with its normal hit chance (which already drops with distance) plus the ability's own modifier: Reflex Shot's facing, Overwatch's -1 to -15 reach penalty. Reflex Shot starts from a flat 33% base instead of Ballistic Skill and deals 33% damage.
[*]Overwatch's and Suppression's shots are real attacks of opportunity: talents and traits that specifically boost attacks of opportunity apply to them. Reflex Shot's are not.
[*]A Suppressed unit's ranged attacks keep 75% of their final hit chance, and the hit-chance gauge shows it.
[*]Overwatch shots carry an extra snap-shot penalty that grows with the cone's reach, -1 to -15, shown both in the hit-chance breakdown and by an Overwatched debuff on everyone the cone currently covers.
[*]Taking any damage breaks concentration: the unit's Overwatch, Reflex Shot or Suppression ends at once.
[*]Overwatch fires at most two shots, and never more than the attacks your AP could have paid for on your own turn.
[*]While you aim Overwatch or Reflex Shot, thin half-transparent lines (yellow for Reflex Shot, blue for Overwatch) trace the area's tile edges and every enemy inside shows the game's own hit-chance gauge for the shot it would take, before you commit.
[*]Being pushed counts as moving: shove an enemy into a Reflex Shot circle or an Overwatch cone and it draws fire.
[*]Overwatch ends when your next turn starts, when you act or when combat ends. An active Overwatch survives saving and loading.
[*]Enemies with pistols, rifles and heavy weapons have the same three abilities. They still fight normally, but when they finish moving with nothing to shoot they overwatch you, and they suppress targets hiding in full cover. Enemies don't steer around your Overwatch cones and Reflex Shot circles: they move as they would anyway and get caught by the shots.
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
