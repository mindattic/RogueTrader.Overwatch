# Design

## Abilities

### Shared rules
- Party members always have the three buttons on their ability bar (slots 8–10 by default), greyed
  out unless the active weapon set holds a ranged weapon. Enemies get them on any ranged weapon
  (Category Pistol, Basic or Heavy, not Thrown) through its weapon slots. See "Who gets the buttons".
- Being pushed counts as moving for all three: the engine's overwatch reacts to any position
  change (`OverwatchController.HandleEntityPositionChanged`), so a push into Reflex Shot's circle or
  Overwatch's cone draws fire, and Suppression fires when its target is pushed off its square.
- Pets never overwatch, even with a ranged weapon: they don't plan ahead. Party pets get no buttons,
  and the enemy AI never casts the abilities for a pet.
- Each shot is the weapon's normal single shot, with normal hit chance: ranged hit chance already
  scales with distance (`RuleCalculateHitChances.OnTriggerRanged` multiplies by
  `RuleCalculateAbilityDistanceFactor`).
- All three cost 1 AP (the blueprints' `ActionPointCost`, and the slot `AP` on enemy weapons), plus 1
  more AP when the active ranged weapon is Heavy (`WeaponHeaviness.Heavy`, RTO-T51): a prefix on
  `RuleCalculateAbilityActionPointCost.OnTrigger` adds to `CostBonus`, the same field the base game's
  own Heavy-only AP surcharges use (`WarhammerOverrideAbilityApCost`, `AffectOnlyHeavy`). Heavy hits
  harder per shot (see Overwatch below), so it costs more to commit to any of the three. They end
  the turn (`WarhammerEndTurn`) and only work in combat (`CombatStateRestriction: InCombatOnly`).
- Overwatch ends when the overwatcher starts its next turn, acts, or combat ends
  (`OverwatchController`).
- **Damage interrupts (RTO-T67):** any real damage (`RuleDealDamage`, not fake previews, result > 0)
  stops the victim's Overwatch or Reflex Shot (`PartOverwatch.Stop`) and removes every Suppressed buff it
  cast (`DamageInterrupt.cs`).
- In-game checks for every rule here are in [TESTING.md](TESTING.md).
- The enemy AI already treats overwatch areas as threats when choosing paths (`AiBrainHelper`
  reads `PartOverwatch.OverwatchArea`). How enemies use the abilities is under "Enemy AI" below.
- **Outline (RTO-T56):** the game's own area highlight is a shared red fill used for every hostile
  area in the game and isn't easy to see, so a postfix on `OverwatchController.Tick`
  (`AbilityOutline_Tick`) draws a bright yellow line around the actual boundary of any caster's active
  Reflex Shot circle or Overwatch cone: the convex hull of `PartOverwatch.OverwatchArea`'s own cells
  (so it's exactly whichever shape is in effect, no separate circle/cone geometry to maintain),
  rendered with a plain `LineRenderer` and Unity's built-in `Sprites/Default` shader — no new art
  asset. Shown both during the live cursor preview while aiming (the same `GetOrientedPattern` prefixes
  that resize the preview also call `AbilityOutline.ShowPreview` with the freshly computed pattern) and
  once the ability is actually active, and removed the instant the overwatch stops. Preview has no
  `PartOverwatch` to key off, so a per-caster timestamp stands in: `GetOrientedPattern` fires every
  frame the cursor moves while targeting, so `AbilityOutline_Tick` hides the preview outline once that
  timestamp goes stale (`PreviewTimeout`, 0.3 s) — covers cancelling the cast, picking another ability,
  or just letting the cursor sit still past a frame or two after backing out.

### Reflex Shot (RTO-T5, RTO-T21)
XCOM 2 overwatch: a circle around the unit, every direction, whose radius is the equipped weapon's own
range (RTO-T62; no fixed size). It fires once at the first enemy that moves or acts inside
(`Mode: Overwatch`, `HitOnce`), then ends. Ends the turn.
- **Radius:** the blueprint's circle (radius 10) is only a placeholder; `ReflexShot_CentreOnShooter`
  rebuilds it with `AoEPattern.CopyAndOverrideRadius` at the weapon's `RangeCells`, and that one prefix
  feeds the preview, `GetOverwatchArea` (the real trigger zone) and the outline. The tooltip's
  "10-cell radius" line is drawn from the static blueprint pattern and can't follow it.
- **Facing:** casting asks for a point within that radius (`Range: Custom`, `CanTargetPoint`,
  `ShouldTurnToTarget`; the click range is set to the weapon's range by a postfix on
  `RuleCalculateAbilityRange.OnTrigger`, `ReflexShot_RangeFromWeapon`); the unit turns to face it, the direction it expects
  enemies from, lateral or diagonal. The circle stays centred on the unit whatever is clicked: a
  prefix on `AbilityAoEPatternSettings.GetOrientedPattern` passes the caster's node as the target node
  (`ReflexShot_CentreOnShooter`), the same seam the preview and `GetOverwatchArea` use.
- **Aim modifier:** the shot's hit chance gets `+10` dead ahead, falling linearly to `-30` for a full
  180-degree spin (0 at 45, -10 at 90, -20 at 135; `ReflexShot.AimBonusAhead`/`AimPenaltyBehind`), plus
  a distance penalty from 0 next to the shooter to `-33` at the weapon's full range
  (`ReflexShot.DistanceModifier`, RTO-T62), summed into the same "Reflex Shot" line.
  The angle is between the facing (unit toward `PartOverwatch`'s saved target point, `m_Target`) and
  the unit-to-target direction on the ground. `Overwatch` mode stops the part as soon as the shot is
  queued, so a prefix on `PartOverwatch.TryTriggerAttack` works the modifier out and keeps it per
  shooter; a prefix on `RulePerformAttackRoll.OnTrigger` (the real attack roll, which triggers its own
  `HitChanceRule` first) adds it to that rule's `HitChanceValueModifiers` with the Reflex Shot ability
  fact as its source (so the breakdown and log show "Reflex Shot") for that shooter and target. UI
  hit-chance previews never see it; the first real roll uses it up, and it lapses after 5 s if the
  attack never rolls, so no later attack (e.g. a Suppression shot) picks it up. The facing comes from
  saved state, so it survives save and load.

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
- **Snap-shot penalty (RTO-T54):** on top of the weapon's normal range falloff, a shot from the cone
  gets an extra hit-chance penalty that grows with how far the cone was aimed: 0 at the minimum reach
  (3 cells), linearly down to -30 at the weapon's full range (`OverwatchDebuff.PenaltyFor`, the same
  magnitude as Reflex Shot's own max aim penalty). The locked-in radius is captured alongside the cone
  resize (`OverwatchCone.LockedRadius`, keyed by shooter). Added to the real roll's
  `HitChanceValueModifiers` under the Overwatch ability's name, the same seam Reflex Shot uses, so it
  shows in the hit-chance breakdown and combat log. Read straight off the shooter's own `PartOverwatch`
  at roll time (it's still active, since Overwatch's `MultipleTargetOverwatch` mode doesn't stop on the
  first hit the way Reflex Shot's does): no pending-shot bookkeeping needed.
- **Overwatched debuff (RTO-T54):** every enemy currently standing in an active Overwatch cone carries
  a visible `Overwatched_Buff`, whose rank is the penalty (percent) that cone's shots carry; a postfix
  on `OverwatchController.Tick` adds, updates and removes it live as enemies cross in or out, or when
  the overwatch ends, independent of the engine's own buff lifecycle. An enemy covered by several
  overwatchers shows the worst (highest) of their penalties. Placeholder icon (no custom art yet).

### Suppression (RTO-T8)
- Target one enemy. Until your next turn, it takes a ranged attack of opportunity (normal hit and
  damage) each time it leaves the square it's standing on, by any means: walking, a charge or jump,
  or being pushed. One shot per command, so a long move is one shot, not one per square. Shooting or
  using abilities in place draws nothing, and other enemies are ignored. Ends the turn.
- For the whole suppression, on every turn, the hero keeps firing at the target as a visual cue only
  (RTO-T66, RTO-T67): the weapon's firing animation and its projectile flying at the target, with
  `SetDoNotDeliverHit` and `SetDoNotPlayHitEffect`, so no hit, damage, ammo, rolls or combat-log lines.
  Between shots the suppressor holds a firing pose (RTO-T69): each shot's attack animation is frozen at
  45% of its length (`AnimationBase.SetSpeed(0)`) until the next shot replaces it, and released when the
  suppression ends or the unit has a real command to play. The first one comes 0.3–0.8 s after the target is Suppressed, then every 0.9–1.5 s
  while the hero is idle and has line of sight; it pauses while the real attack of opportunity plays.
- The real attack only fires if the target can still be targeted (`CanTarget`: range and line of
  sight). Several suppressors each fire once per move.
- **Tether (RTO-T65):** while the Suppressed buff lasts, a glowing red line runs from each suppressor's
  eyes to the target's chest (`SuppressionLine.cs`, synced every `OverwatchController.Tick` from the
  buffs themselves): a thin bright core over a wide translucent halo, additive-blended so it reads as a
  glow, pulsing slowly. No asset, Unity's built-in `Hidden/Internal-Colored` shader.
- How it's built (`Suppression.cs`): the ability applies a visible **Suppressed** buff to the
  target (1 round, `BuffEndCondition.TurnStartOrCombatEnd`, which `BuffsController` removes when
  the buff's *caster* starts its turn). It doesn't use `PartOverwatch`, so the target's actions
  can't trigger it. The target's square is recorded when the buff is added (`IUnitBuffHandler`), and
  rebuilt from the saved buffs after a load. When a position change (`IEntityPositionChangedHandler`)
  puts it on a different square, one shot per suppressor is queued, unless the target's current
  command already drew one; a displacement with no command (a push) always draws one. A postfix on `OverwatchController.Tick` fires the queue as `UnitOverwatchAttackParams`,
  the engine's own out-of-turn path, so hit chance and damage are normal. The ability data is a fresh
  `Clone(false)` of the weapon's attack, not the shared instance the unit also fires its normal shots
  with, flagged `IsAttackOfOpportunity = true` (RTO-T50) so feats and traits that key off that flag
  (`CheckIfAttackOfOpportunityGetter`, `AttackOfOpportunityModifier`) apply here too, matching the
  "ranged attack of opportunity" description above — they didn't before, despite the description. The same tick runs the
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
skips them. So `PartyGrant` (Main.cs) gives every non-pet character in `Player.AllCharacters` (on area load and on
recruit) `Overwatch_PartyFeature`, an `AddFacts` of the three abilities, and removes `Overwatch_Feature`
from anyone an earlier build gave it to.
- **Greyed out without a gun:** every ability has `AbilityCasterHasWeaponWithRangeType: Ranged`, which
  checks the active set's main hand, off hand and extra limbs. The bar re-reads availability on weapon-set
  swaps (`IUnitActiveEquipmentSetHandler`) and when the inventory and other full-screen windows close.
  Melee only, two melee weapons or nothing in the hands greys all three out; a gun in either hand lights them.
  One quirk of that game component: a hand weapon with any burst-delivery ability also passes it. The
  other is confirmed, not just a risk (RTO-T53): its extra-limb loop reads `additionalLimb.Weapon` even
  when the limb is empty, throwing a `NullReferenceException` for any caster with an unfilled additional
  limb — confirmed in game on Pasqal, who has mechadendrites, and it greys out all three regardless of
  what's in his hands. A prefix on `AbilityCasterHasWeaponWithRangeType.IsCasterRestrictionPassed`,
  scoped to our three abilities only (`Abilities.IsOurs(OwnerBlueprint)`), redoes the same check with the
  empty-limb case skipped instead of dereferenced; every other ability in the game keeps hitting the
  original, unpatched method. Out of combat they're unusable (`InCombatOnly`).
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
  (1 = Edit, which applies `<Filename>.jbp_patch`). It must also carry empty
  `BundlesLayout: {m_Guids: [], m_Bundles: []}` and `BundleDependencies: {m_List: []}`: the loader
  reads it with Unity's `JsonUtility`, which leaves missing sections null, and every scene load asks
  each mod for bundles through `Settings.BundlesLayout.BundleNames`, so without them the game hangs
  on its first loading screen (NullReferenceException in `OwlcatModification.TryLoadBundleAsync`).
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
