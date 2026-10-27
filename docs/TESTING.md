# Testing in game

A checklist for confirming the mod works, one case at a time. Every expected result below matches what
the code does today; if the game shows something else, that's a bug worth a ticket. Note each result in
the table at the end.

## Setup

1. Build and install: `dotnet build src/RogueTrader.Overwatch`. This writes the mod to
   `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\RogueTrader.Overwatch`
   and adds it to `EnabledModifications` in `OwlcatModificationManagerSettings.json` in the same folder.
2. Start the game (or restart it, if it was running).
3. Logs live in `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\`:
   `Player.log` (Unity) and `GameLogFull.txt` (the game's own log). Search both for:
   - `RogueTrader.Overwatch` and `Suppression:`: every error the mod's code catches starts with one of
     these. There should be none.
   - `Failed loading blueprint`, `Patch file not found`, `Failed patch blueprint`: the mod loader
     couldn't read one of the mod's files.
   - Expected, healthy lines: `Initialize assembly: RogueTrader.Overwatch`, `Load blueprint:` once per
     file in `Blueprints\`, and `Patching blueprint: ... (ec16ff5722314efd887fa64f6f6e2558)` (the
     enemies' shared fact) once a save is loaded.

Abbreviations: **AP** = action points (yellow), **MP** = movement points (blue), **cell** = one grid
square.

## A. Install and loading

**A1. The mod loads.** Start the game, open Owlcat's mod window (its key is listed under Settings,
Controls, General).
Expected: "Overwatch" by MindAttic is listed and enabled. The log has the healthy lines above and no errors.

**A2. Loading a save.** Load any save.
Expected: no `RogueTrader.Overwatch` errors in the log.

## B. Ability bar placement (party)

The bar is 10 slots per row. The mod places each of Reflex Shot, Overwatch and Suppression that isn't
already on a character's bar: first into free slots among slots 8–10 of row 1, then the last three
slots of row 2, then of row 3, then the first free slot anywhere. It runs when an area loads, when a
companion is recruited and whenever the bar is refreshed for a character, whatever the game's
"auto-fill action bar" setting says.

**B1. Existing save.** Load a mid-game save, select each party member in turn.
Expected: every party member shows the three buttons in slots 8, 9 and 10 of row 1 (if those were
empty), with the custom icons.

**B2. New game.** Start a new game and reach the first fight.
Expected: as B1 for the Rogue Trader, and for each companion as they join.

**B3. Recruited companion.** Recruit a companion (or load a save just before a recruitment).
Expected: the new companion has the three buttons straight away, placed as in B1.

**B4. Mercenary.** Hire a mercenary (custom companion) and add them to the party.
Expected: they have the three buttons, placed as in B1, after the next area load at the latest.

**B4a. Pets.** With a pet in the party (including one with a ranged weapon), select it.
Expected: no Reflex Shot, Overwatch or Suppression buttons on its bar.

**B5. Row 1 full.** Drag three other abilities onto slots 8–10, replacing the three buttons. Select
another character and back (or change area).
Expected: the three buttons reappear in the free slots among the last three of row 2 (slots 18–20); if
those are taken too, row 3 (28–30); otherwise the first free slot. The bar doesn't grow extra empty rows.
(If they don't reappear at all, the game counted the replace as a removal: see B6, and note it.)

**B6. Removing a button.** Drag one of the three buttons off the bar. Change area, save and load.
Expected: it stays off. The mod never re-adds a button the player removed. (Drag it back from the
character's abilities to restore it.)

**B7. Moving a button.** Drag a button to another slot. Change area, save and load.
Expected: it stays where you put it, and isn't duplicated.

## C. Greyed out without a gun

The buttons are usable only when the **active weapon set** holds a ranged weapon (pistol, basic or
heavy) in the main hand, the off hand or an extra limb. Otherwise they're greyed out. They're also
unusable out of combat. Check each case in combat, on a party member's ability bar:

| # | Active weapon set | Expected |
|---|---|---|
| C1 | Rifle (basic, two-handed) | Lit |
| C2 | Pistol | Lit |
| C3 | Heavy weapon | Lit |
| C4 | One melee weapon | Greyed |
| C5 | Two melee weapons | Greyed |
| C6 | Sword in main hand, pistol in off hand | Lit; shots use the **pistol** |
| C7 | Nothing (unarmed) | Greyed |
| C8 | Grenades or other thrown weapons only | Greyed |
| C9 | A character with mechadendrites (e.g. Pasqal) holding only melee weapons | Greyed, and no errors in the log |

**C10. Swapping weapon sets.** Set 1 a rifle, set 2 a sword. Swap sets in combat.
Expected: the buttons grey out and light up with each swap, immediately.

**C11. Unequipping mid-combat.** In combat, open the inventory, unequip every ranged weapon from the
active set, close the inventory.
Expected: greyed as soon as the inventory closes. Re-equip a gun: lit again.

**C12. Out of combat.** Walk around outside combat.
Expected: the buttons are on the bar but can't be used.

## D. Reflex Shot

Ends your turn. Costs 1 AP. You pick the direction you expect enemies from; until your next turn it
fires once at the first enemy that moves or acts within 10 cells of you, in any direction, then it ends.
The shot's hit chance gets a "Reflex Shot" modifier for how far you have to turn: +10 straight ahead,
0 at 45°, -10 at 90°, -20 at 135°, -30 directly behind.

**D1. Cast.** In combat, with a gun, press Reflex Shot and click a point (within 10 cells) to face.
Expected: your character turns to face it; the highlighted area is a 10-cell circle centred on you, not on
the clicked point; no shot on cast; your turn ends.

**D2. Enemy moves in range.** Let an enemy move within 10 cells.
Expected: one shot at it, with the weapon's normal hit chance and damage (shown in the combat log like a
normal attack) plus the Reflex Shot modifier. No further shots this round, even if other enemies move.

**D2a. Turn modifier.** Repeat D2 with the enemy coming from straight ahead, from the side (90°) and from
behind (180°) relative to the facing you picked.
Expected: the hit-chance breakdown in the combat log shows "Reflex Shot" +10 ahead, -10 from the side
and -30 from behind (between those, it scales evenly: 0 at 45°, -20 at 135°).

**D2b. Facing survives a reload.** Cast Reflex Shot facing one way, save, load, then let an enemy come
from behind.
Expected: still -30.

**D2c. Pushed into range.** Push an enemy (any push or knockback ability) from outside 10 cells to inside.
Expected: the Reflex Shot fires at it, the same as if it had walked in.

**D3. Enemy acts in range without moving.** Let an enemy within 10 cells shoot or use an ability in place.
Expected: one shot at it (acting also triggers Reflex Shot), then it ends.

**D4. Enemy outside 10 cells.** Only enemies more than 10 cells away move.
Expected: no shot.

**D5. Ends at your turn.** Nobody triggers it before your next turn.
Expected: it's gone when your turn starts.

## E. Overwatch

Ends your turn. Costs 1 AP. **Shots = the AP you had before casting** (at least 1). The cone is sized by
the cursor: from 3 cells up to your weapon's range, 90° wide when close down to 20° at full range.

**E1. Drag the cone.** Press Overwatch, move the cursor close to the character, then far away.
Expected: the highlighted cone grows longer and narrower as the cursor moves away, shorter and wider as
it comes close. It never gets shorter than 3 cells or longer than the weapon's range.

**E2. Locked cone matches the preview.** Click to cast.
Expected: no shot on cast, your turn ends, and the zone shown afterwards matches the preview you clicked.

**E3. Empty cone.** Cast Overwatch where no enemy is inside the cone.
Expected: it still starts (shots-left buff appears, see E8).

**E4. Shots = AP.** Cast with 3 AP left. Let three or more enemies walk through the cone.
Expected: at most 3 shots in total, then it ends and the buff disappears.

**E5. Spread fire.** Two enemies in the cone; enemy A moves first and is shot, then A moves again 2+
cells while enemy B is still in the cone unshot.
Expected: A's second move draws no fire. When B moves (or acts), B is shot.

**E6. 2-cell re-shot.** One enemy alone in the cone, 2+ shots left. It walks 6 cells through the cone.
Expected: shots at entry, after 2 cells and after 4 cells (until shots run out), not one per cell.

**E6a. Pushed into the cone.** Push an enemy into the cone, or push one already shot 2+ cells within it.
Expected: a shot, the same as walking in; a push of 2+ cells counts toward the re-shot distance.

**E7. Acting in place.** An enemy already shot stays put and shoots or uses an ability.
Expected: no second shot at it. (An enemy *not yet* shot that acts in the cone does draw a shot.)

**E8. Shots-left buff.** Cast with 3 AP.
Expected: an "Overwatch" buff on the shooter with the Overwatch-shots icon, showing 3; it counts down
with each shot and disappears with the last one or at your next turn.

**E9. Save and load mid-overwatch.** Cast with 3 AP, let one shot fire (buff shows 2), save, load.
Expected: the buff still shows 2 and the overwatch still fires 2 more shots. (Known: which enemies were
already shot isn't saved, so after loading every enemy in the cone counts as not yet shot.)

**E10. Ends at your turn.** Shots left when your next turn starts.
Expected: the overwatch and the buff end.

## F. Suppression

Ends your turn. Costs 1 AP. Target one enemy within weapon range. Until your next turn, leaving the
square it stands on, by any means, draws one real attack from you (once per move or ability, not per
square); acting in place draws nothing.

**F1. Cast.** Press Suppression, pick an enemy in range.
Expected: no shot on cast, your turn ends, the enemy gets the "Suppressed" debuff with its icon.

**F2. Target moves.** On its turn the suppressed enemy moves.
Expected: one attack at it, early in the move, with normal hit chance and damage. If it moves again
later in its turn, one more attack for that move (one per move, not one per cell).

**F2a. Leaves by ability or push.** The suppressed enemy charges or jumps, or is pushed off its square.
Expected: one attack, the same as a move.

**F3. Target acts in place.** It shoots or uses an ability without moving.
Expected: no attack.

**F4. Other enemies.** A different enemy moves near you.
Expected: no attack from Suppression.

**F5. Cosmetic fire.** During the suppressed enemy's turn, watch the hero.
Expected: about a second after its turn starts, then every 1.5–2.5 s while the hero is idle and can see
it, the hero plays an attack animation and the weapon's projectiles fly past the target, wide and high.
No hit effects, no damage, no ammo used, no combat-log lines. Pauses while a real attack (F2) plays.

**F6. Out of range or sight.** The target moves out of the hero's weapon range or line of sight.
Expected: no attack while it can't be targeted, and no cosmetic fire without line of sight.

**F7. Ends at your turn.** Your next turn starts.
Expected: the Suppressed debuff is gone.

**F8. Two suppressors.** Two party members suppress the same enemy; it moves.
Expected: one attack from each.

## G. Enemies

Enemies get the three abilities on their ranged weapons (in the weapon section, not an ability bar), and
the AI uses them only once it has finished moving and hasn't spent any AP this turn, with a ranged
weapon in its main hand:

**G1. Normal turns unchanged.** Enemies that can shoot someone in the open.
Expected: they shoot normally. No overwatch.

**G2. Overwatch when nothing to shoot.** An enemy with 2+ AP ends its move with no one it can shoot, but
a party member within its weapon range + 8 cells.
Expected: it casts Overwatch, a cone aimed at the nearest party member and reaching toward them (capped
at its weapon range), and its turn ends. Walking into the cone draws its shots.

**G3. Reflex Shot with 1 AP.** Same, but the enemy has only 1 AP (or Overwatch can't be aimed).
Expected: Reflex Shot instead.

**G4. Suppression when everyone is in full cover.** An enemy can target party members, but every one of
them is in full cover.
Expected: it suppresses the nearest one not already suppressed. That character then draws a shot each
time they move.

**G5. Nobody near.** No party member within weapon range + 8 cells.
Expected: no overwatch; the enemy's turn ends as before.

**G6. Avoiding overwatch.** Put Overwatch or Reflex Shot down, then end your turn.
Expected: enemies tend to path around the overwatched area (the game's own AI already treats it as a
threat).

**G6a. Enemy pets.** A fight with enemy animals or pets, ideally one with a ranged attack.
Expected: they never use Reflex Shot, Overwatch or Suppression.

**G7. Allied NPCs.** In a fight with allied non-party units holding guns.
Expected: they follow the same rules as enemies (G1–G5), against their own enemies. Watch for anything odd.

## H. Icons

**H1.** Check every icon: the three buttons (Reflex Shot, Overwatch, Suppression), the shots-left buff
(E8) and the Suppressed debuff (F1).
Expected: the custom art from `assets/`, not the game's borrowed placeholders (Take Aim, Killzone, etc.).
Tooltips show the names and descriptions from `mod/Localization/enGB.json`.

## I. Uninstall

**I1.** Outside combat, disable the mod in the mod window, restart, load the save.
Expected: the save loads. The buttons are gone. Note any errors in the log.

## Results

| Case | Pass / Fail | Notes |
|---|---|---|
| A1 |  |  |
| A2 |  |  |
| B1 |  |  |
| B2 |  |  |
| B3 |  |  |
| B4 |  |  |
| B4a |  |  |
| B5 |  |  |
| B6 |  |  |
| B7 |  |  |
| C1–C9 |  |  |
| C10 |  |  |
| C11 |  |  |
| C12 |  |  |
| D1 |  |  |
| D2 |  |  |
| D2a |  |  |
| D2b |  |  |
| D2c |  |  |
| D3 |  |  |
| D4 |  |  |
| D5 |  |  |
| E1 |  |  |
| E2 |  |  |
| E3 |  |  |
| E4 |  |  |
| E5 |  |  |
| E6 |  |  |
| E6a |  |  |
| E7 |  |  |
| E8 |  |  |
| E9 |  |  |
| E10 |  |  |
| F1 |  |  |
| F2 |  |  |
| F2a |  |  |
| F3 |  |  |
| F4 |  |  |
| F5 |  |  |
| F6 |  |  |
| F7 |  |  |
| F8 |  |  |
| G1 |  |  |
| G2 |  |  |
| G3 |  |  |
| G4 |  |  |
| G5 |  |  |
| G6 |  |  |
| G6a |  |  |
| G7 |  |  |
| H1 |  |  |
| I1 |  |  |
