# Design

## Abilities

### Shared rules
- Party members always have the three buttons on their ability bar (slots 8–10 by default), greyed
  out unless the active weapon set holds a ranged weapon. Enemies get them on any ranged weapon
  (Category Pistol, Basic or Heavy, not Thrown) through its weapon slots. See "Who gets the buttons".
- Each shot is the weapon's normal single shot, with normal hit chance: ranged hit chance already
  scales with distance (`RuleCalculateHitChances.OnTriggerRanged` multiplies by
  `RuleCalculateAbilityDistanceFactor`).
- All three cost 1 AP (the blueprints' `ActionPointCost`, and the slot `AP` on enemy weapons), end
  the turn (`WarhammerEndTurn`) and only work in combat (`CombatStateRestriction: InCombatOnly`).
- Overwatch ends when the overwatcher starts its next turn, acts, or combat ends
  (`OverwatchController`).
- In-game checks for every rule here are in [TESTING.md](TESTING.md).
- The enemy AI already treats overwatch areas as threats when choosing paths (`AiBrainHelper`
  reads `PartOverwatch.OverwatchArea`). How enemies use the abilities is under "Enemy AI" below.

### Reflex Shot (RTO-T5, RTO-T21)
XCOM 2 overwatch: a 10-cell circle around the unit, every direction. It fires once at the first enemy
that moves or acts inside (`Mode: Overwatch`, `HitOnce`), then ends. Ends the turn.
- **Facing:** casting asks for a point within 10 cells (`Range: Custom`, `CustomRange: 10`,
  `CanTargetPoint`, `ShouldTurnToTarget`); the unit turns to face it, the direction it expects
  enemies from, lateral or diagonal. The circle stays centred on the unit whatever is clicked: a
  prefix on `AbilityAoEPatternSettings.GetOrientedPattern` passes the caster's node as the target node
  (`ReflexShot_CentreOnShooter`), the same seam the preview and `GetOverwatchArea` use.
- **Aim modifier:** the shot's hit chance gets `+10` dead ahead, falling linearly to `-30` for a full
  180-degree spin (0 at 45, -10 at 90, -20 at 135; `ReflexShot.AimBonusAhead`/`AimPenaltyBehind`).
  The angle is between the facing (unit toward `PartOverwatch`'s saved target point, `m_Target`) and
  the unit-to-target direction on the ground. `Overwatch` mode stops the part as soon as the shot is
  queued, so a prefix on `PartOverwatch.TryTriggerAttack` works the modifier out and keeps it per
  shooter; a prefix on `RuleCalculateHitChances.OnTrigger` adds it to `HitChanceValueModifiers` with
  the Reflex Shot ability fact as its source (so the breakdown and log show "Reflex Shot") when the
  shooter's current command is that `UnitOverwatchAttack` at that target. The facing comes from saved
  state, so it survives save and load.

### Overwatch (RTO-T6)
- After pressing the button the cone resizes live with the cursor: farther gives a longer, narrower
  cone and closer a shorter, wider one. `OverwatchCone.ConeFor`: radius = the caster→cursor
  distance clamped to 3 cells … weapon range; angle runs linearly from 90° at 3 cells to 20° at
  weapon range. A prefix on `AbilityAoEPatternSettings.GetOrientedPattern` serves both the
  targeting preview and `GetOverwatchArea`, so the locked zone matches the preview. Click to lock it in.
- Casting costs 1 AP and ends the turn. **Shots = the AP the unit had before casting** (read in a
  prefix on `UnitUseAbility.OnStart`, before the cost is paid; at least 1).
- Enemies that move or act in the cone draw shots until the shots run out or the next turn starts.
- The shots left are the rank of an "Overwatch" buff on the shooter (`OverwatchShots_Buff`), which
  the engine adds and removes with the overwatch (`AbilityEffectOverwatch.ApplyingBuffs`). The
  rank shows the player the count and is saved with the game. The re-shot positions aren't saved:
  after a reload, every enemy in the cone counts as not yet shot.
- **Spread fire first:** while any enemy in the cone hasn't been shot yet, an already-shot enemy
  draws no fire.
- **Re-shots:** when there's no other choice, an already-shot enemy can be shot again, but only
  after it has moved at least 2 cells since its last shot (`MinReshotDistance`, grid distance).
- **Acting in place:** an enemy not yet shot that acts in the cone draws a shot (the engine's own
  trigger); an already-shot enemy acting without moving never draws another one.
- The rules live in a prefix and postfix on `PartOverwatch.TryTriggerAttack` (the cast uses
  `MultipleTargetOverwatch` + `HitManyTimes` so the engine never stops or skips on its own).

### Suppression (RTO-T8)
- Target one enemy. Until your next turn, it takes a ranged attack of opportunity (normal hit and
  damage) each time it leaves the square it's standing on, by any means: walking, a charge or jump,
  or being pushed. One shot per command, so a long move is one shot, not one per square. Shooting or
  using abilities in place draws nothing, and other enemies are ignored. Ends the turn.
- During that enemy's turn the hero keeps up cosmetic fire: shoot animation and the weapon's
  projectiles, always aimed to miss, with no damage, hit rolls, ammo or combat-log lines. The first
  burst comes 0.6–1.2 s into the turn, then every 1.5–2.5 s while the hero is idle and has line of
  sight. It pauses while the real attack of opportunity plays.
- The real attack only fires if the target can still be targeted (`CanTarget`: range and line of
  sight). Several suppressors each fire once per move.
- How it's built (`Suppression.cs`): the ability applies a visible **Suppressed** buff to the
  target (1 round, `BuffEndCondition.TurnStartOrCombatEnd`, which `BuffsController` removes when
  the buff's *caster* starts its turn). It doesn't use `PartOverwatch`, so the target's actions
  can't trigger it. The target's square is recorded when the buff is added (`IUnitBuffHandler`), and
  rebuilt from the saved buffs after a load. When a position change (`IEntityPositionChangedHandler`)
  puts it on a different square, one shot per suppressor is queued, unless the target's current
  command already drew one; a displacement with no command (a push) always draws one. A postfix on `OverwatchController.Tick` fires the queue as `UnitOverwatchAttackParams`,
  the engine's own out-of-turn path, so hit chance and damage are normal. The same tick runs the
  cosmetic fire: `UnitAnimationManager.CreateHandle(MainHandAttack)`, then a `ProjectileLauncher`
  aimed 30 m out past a point 1.6–2.4 m beside the target, with `SetDoNotDeliverHit`, using Unity's
  random so the game's seeded random and state hash are untouched.
- Data alone was close but not enough: `MovedInCombatTrigger` fires only after a move ends, and
  `ContextActionAttackWithFirstWeaponAbility` queues the attack instead of firing out of turn.

Dropped: Lane Overwatch (RTO-T9). Kill Zone (RTO-T7) and the drag-to-size cone (RTO-T12) merged
into Overwatch.

### Enemy AI (RTO-T11)
Enemies get the buttons through `CommonMobFact`, and the AI considers every ability a unit has
(`DecisionContext.GetSortedAbilityList`). Left alone it sorts end-turn abilities last
(`IsFirstAbilityBetter`) but still casts them on leftover AP: Suppression goes through
`SingleTargetSelector` at any shootable enemy, and Overwatch through `AOETargetSelector`, which
only aims at enemies already inside the cone. So `EnemyAi.cs` hides the three from that search
(postfix on `DecisionContext.IsUsableAbility`) and decides in one place (prefix on
`TaskNodeSelectAbilityTarget.TickInternal`). That runs only for non-player units in turn-based
combat that hold a ranged weapon, haven't spent any AP this turn, and are done moving: no movement
points left, or the after-move pick.
- Some enemy can be shot, but every one is in full cover: **Suppression** on the nearest one not
  already suppressed ("can't get a good shot").
- Some enemy can be shot in the open: nothing changes, the normal attack runs.
- No enemy can be shot, but one is within weapon range + 8 cells (`ApproachMargin`): **Overwatch**
  aimed at the nearest one (the target point sits at its distance, capped at weapon range, so the
  resize patch sizes the cone to reach it) with 2+ AP, or **Reflex Shot** with 1 AP (or when the
  cone can't be aimed), facing the nearest enemy, where one shot in a full circle beats one shot in a cone.
- The ranged-weapon check uses the main hand (`GetFirstWeapon`), so an enemy with a sword in the
  main hand and a pistol in the off hand doesn't overwatch. The rule covers every non-player unit,
  allied NPCs included.
- Otherwise nothing, and the turn ends as before.

## The engine's overwatch (Code.dll)

- `AbilityEffectOverwatch` (TypeId `0966a95df3bfa574e855a652cc54afa2`) on an ability creates a
  `PartOverwatch` on the caster with a zone from the ability's attack pattern
  (`GetOverwatchArea` → `GetPatternSettings().GetOrientedPattern(ability, casterNode, targetNode)`).
  Fields: `Mode` (`Overwatch`, `SuppressionFire`, `MultipleTargetOverwatch`), `HitsPerTarget`
  (`HitOnce`, `HitManyTimes`), `UseFirstWeaponAbility` (fire the weapon's first ability) or
  `m_AbilityOnTrigger`, and `ApplyingBuffs` (buffs on the caster while it's active).
- `PartOverwatch.TryTriggerAttack` runs a `UnitOverwatchAttack` command and, in `Overwatch` mode
  only, stops after the shot. `SuppressionFire` and `MultipleTargetOverwatch` behave the same in
  code. `HitOnce` remembers units already shot.
- `OverwatchController` collects triggers from `HandleEntityPositionChanged` (every position update,
  many per move) and `HandleUnitCommandDidAct` (abilities whose `UsingInOverwatchArea` is
  `WillCauseAttack`; every ability declares it), and stops a unit's overwatch when it starts its
  turn or runs any other command.
- Unused content shipped in the game: `Weapons/Abilities/Overwatch_BasicAbility`
  (`1d271e9472c142ecaa86103caa940010`, 2 AP, live but referenced by nothing, no strings or icon)
  with cone `OverwatchCone_AttackTemplate` (`8fbb90e7352a4aab843d913ac5ddc00a`, radius 8, 30°). A
  cut Veteran version offered cone or line variants (all `ShadowDeleted`).
- Pattern shapes: `BlueprintAttackPattern` with `Type` `Cone`, `Circle`, `Ray` or `Custom`, plus
  `m_Radius` and `m_Angle`. At runtime `AoEPattern.Cone(angle, radius)`, `AoEPattern.Circle(radius)`
  and `AoEPattern.CopyAndOverrideRadius` build any size.

## Who gets the buttons, and where (RTO-T4, RTO-T18)

**Enemies: on the weapon (RTO-T4).** Every non-player unit gets `BlueprintWarhammerRoot.CommonMobFact`
(`BaseUnitEntity.OnInitialize`). An `AddFacts` patched onto it gives them `Overwatch_Feature`, whose
`AddAbilitiesToCurrentWeapon` components (TypeId `b787865cc7284095914908b39df0a903`, one per ranged
category: Pistol, Basic, Heavy) add the three abilities as weapon slots on the active weapon. The
enemy AI (RTO-T11) works from those. `AddFacts` restores missing facts on load, so units in existing
saves get it too.

**Party: their own abilities, on the ability bar (RTO-T18).** Weapon-slot abilities only show in the
weapon's section and vanish with the weapon, and the bar's auto-fill (`PartUnitUISettings.TryToInitialize`)
skips them. So `PartyGrant` (Main.cs) gives every character in `Player.AllCharacters` (on area load and on
recruit) `Overwatch_PartyFeature`, an `AddFacts` of the three abilities, and removes `Overwatch_Feature`
from anyone an earlier build gave it to.
- **Greyed out without a gun:** every ability has `AbilityCasterHasWeaponWithRangeType: Ranged`, which
  checks the active set's main hand, off hand and extra limbs. The bar re-reads availability on weapon-set
  swaps (`IUnitActiveEquipmentSetHandler`) and when the inventory and other full-screen windows close.
  Melee only, two melee weapons or nothing in the hands greys all three out; a gun in either hand lights them.
  Two quirks of that game component, untested: a hand weapon with any burst-delivery ability also passes
  it, and its extra-limb loop reads `additionalLimb.Weapon` even when the limb is empty (a possible
  error for characters with mechadendrites; see TESTING.md C9). Out of combat they're unusable
  (`InCombatOnly`).
- **Placement** (`ActionBar.cs`): slots 8–10 of the first 10-slot row (the bar is
  `PartUnitUISettings.Slots`), else the free slots among the last three of row 2, then row 3, else the
  first free slot. Runs on grant and as a prefix on `PartUnitUISettings.TryToInitialize`, which the bar
  calls just before reading its slots, whether or not the game's auto-fill setting is on. An ability the
  player removed from the bar (`m_RemovedFromActionBar`) stays off.
- **Range:** a character-owned ability has `AbilityData.Weapon == null`, so `Range: Weapon` resolved to
  nothing. A postfix on `RuleCalculateAbilityRange.OnTrigger` uses the active ranged weapon's attack range
  instead; Overwatch's cone cap and Suppression's target range follow it.
- **Which gun fires:** `Abilities.RangedWeapon` picks the first ranged weapon in the active set (main hand,
  off hand, extra limbs). `UseFirstWeaponAbility` would take `GetFirstWeapon`, the melee main hand when a
  sword and pistol are held, so a postfix on `AbilityEffectOverwatch.Apply` restarts the overwatch on the
  ranged weapon's attack; Suppression's real and cosmetic shots use it too.

**Cast delivery (both).** `WarhammerAbilityAttackDelivery`'s pattern delivery applies the ability's
effects once per unit inside the area (`AbilityExecutionProcess.TickDeliveryProcess`): an empty zone never
started overwatch, and several enemies restarted it once each. A prefix on its `Deliver` makes Reflex Shot
and Overwatch deliver exactly once, to the clicked target. `WeaponAttack: None` already means no attack
is made on cast.

## Mod packaging (RTO-T3)

An Owlcat modification folder, assembled by `dotnet` with no Unity:
- `OwlcatModificationManifest.json` (UniqueName, Version, DisplayName, Description, Author).
- `OwlcatModificationSettings.json` holding `BlueprintPatches: [{Guid, Filename, PatchType}]`
  (1 = Edit, which applies `<Filename>.jbp_patch`).
- `Blueprints/**/*.jbp` (new blueprints) and `*.jbp_patch`. Patch fields: `FieldOverrides` take
  dotted paths, `ComponentsPatches` add components, `ArrayPatches` edit reference arrays
  (`Code.GameCore.Blueprints.BlueprintPatcher`).
- `Localization/enGB.json` as `{"strings": {"<key>": {"Text": "..."}}}`, the game's own pack format
  (`LocalizationPack`). The array format in Owlcat's template example doesn't load.
- `Assemblies/RogueTrader.Overwatch.dll`: loaded by the game, which calls the one static method
  marked `[OwlcatModificationEnterPoint]`. It applies Harmony patches with the game's own
  `0Harmony.dll` for the parts data can't express: the drag cone, the shot budget and re-shot
  rules, Suppression's movement-only trigger and its cosmetic fire.
- Enable it by adding its UniqueName to `EnabledModifications` in
  `OwlcatModificationManagerSettings.json`.

Icons (RTO-T10): `assets/<Name>.png` (square PNG; the current set is 64×64 with alpha) is copied to
the mod's `Icons/` folder. After the game loads the mod's blueprints
(`OwlcatModification.ApplyContent`), `Icons.cs` turns each into a `Sprite` and sets the blueprint's
private `BlueprintMechanicEntityFact.m_Icon`, so no Unity asset bundle is needed. The `m_Icon`
references in the `.jbp` files are game sprites that stay as fallbacks if a PNG is missing. Files:
`ReflexShot`, `Overwatch`, `OverwatchShots` (shots-left buff), `Suppression`, `Suppressed` (debuff).
