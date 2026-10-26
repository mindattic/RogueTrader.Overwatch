# Design

## Abilities

### Shared rules
- Every unit (hero or enemy) holding a ranged weapon, meaning Category Pistol, Basic or Heavy (not
  Thrown), gets the buttons on that weapon's action bar. A melee weapon hides them.
- Each shot is the weapon's normal single shot, with normal hit chance: ranged hit chance already
  scales with distance (`RuleCalculateHitChances.OnTriggerRanged` multiplies by
  `RuleCalculateAbilityDistanceFactor`).
- Overwatch ends when the overwatcher starts its next turn, acts, or combat ends
  (`OverwatchController`).
- The enemy AI already treats overwatch areas as threats when choosing paths (`AiBrainHelper`
  reads `PartOverwatch.OverwatchArea`). How enemies use the abilities is under "Enemy AI" below.

### Reflex Shot (RTO-T5)
XCOM 2 overwatch. No aiming: a circle around the unit (start at 10 cells). It fires once at the
first enemy that moves or acts inside, then ends. Ends the turn. Data only.

### Overwatch (RTO-T6)
- After pressing the button the cone resizes live with the cursor: farther gives a longer, narrower
  cone and closer a shorter, wider one, capped at weapon range. Click to lock it in.
- Casting spends all remaining AP (at least 1) and ends the turn. **Shots = the AP left at cast.**
- Enemies that move or act in the cone draw shots until the shots run out or the next turn starts.
- The shots left are the rank of an "Overwatch" buff on the shooter (`OverwatchShots_Buff`), which
  the engine adds and removes with the overwatch (`AbilityEffectOverwatch.ApplyingBuffs`). The
  rank shows the player the count and is saved with the game. The re-shot positions aren't saved:
  after a reload, every enemy in the cone counts as not yet shot.
- **Spread fire first:** while any enemy in the cone hasn't been shot yet, an already-shot enemy
  draws no fire.
- **Re-shots:** when there's no other choice, an already-shot enemy can be shot again, but only
  after it has moved at least 2 tiles since its last shot (tunable).
- Open: can an already-shot enemy that acts without moving draw a second shot? Proposed: no, only
  movement re-triggers.

### Suppression (RTO-T8)
- Target one enemy. Until your next turn, it takes a ranged attack of opportunity (normal hit and
  damage) each time it moves. Shooting or using abilities in place draws nothing, and other
  enemies are ignored. Ends the turn.
- During that enemy's turn the hero keeps up cosmetic fire: shoot animation and the weapon's
  projectiles, always aimed to miss, with no damage, hit rolls, ammo or combat-log lines. It pauses
  while the real attack of opportunity plays.
- How it's built (`Suppression.cs`): the ability applies a visible **Suppressed** buff to the
  target (1 round, `BuffEndCondition.TurnStartOrCombatEnd`, which `BuffsController` removes when
  the buff's *caster* starts its turn). It doesn't use `PartOverwatch`, so the target's actions
  can't trigger it. When the target starts a move command (`UnitMoveTo`, `UnitMoveToProper`,
  `UnitMoveAlongPath`, `UnitMoveContinuously`), its first position change queues one shot per
  suppressor. A postfix on `OverwatchController.Tick` fires the queue as `UnitOverwatchAttackParams`,
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
- No enemy can be shot, but one is within weapon range + 8 cells: **Overwatch** aimed at the nearest
  one (the target point sits at its distance, capped at weapon range, so the resize patch sizes
  the cone to reach it) with 2+ AP, or **Reflex Shot** with 1 AP, where one shot in a full circle
  beats one shot in a cone.
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

## Putting the buttons on ranged weapons (RTO-T4)

- `AddAbilitiesToCurrentWeapon` (TypeId `b787865cc7284095914908b39df0a903`, allowed on
  `BlueprintUnitFact` and `BlueprintUnit`) adds `WeaponAbility` slots to the wielder's active
  weapon when its Category, Family, Classification and HoldingType match. Use one component per
  ranged category.
- Enemies: every non-player unit gets `BlueprintWarhammerRoot.CommonMobFact`
  (`BaseUnitEntity.OnInitialize`). Patching components onto that fact reaches every enemy,
  including ones in existing saves.
- Heroes: there's no shared player fact; RTO-T4 picks a route (the C# assembly adding a fact to
  party members, or patching hero unit blueprints).
- Fallback: weapon slot 5 (`WeaponAbilities.Ability5`) is free on all but 4 ranged weapons, and
  they inherit it from 38 prototypes.

## Mod packaging (RTO-T3)

An Owlcat modification folder, assembled by `dotnet` with no Unity:
- `OwlcatModificationManifest.json` (UniqueName, Version, DisplayName, Description, Author).
- `OwlcatModificationSettings.json` holding `BlueprintPatches: [{Guid, Filename, PatchType}]`
  (1 = Edit, which applies `<Filename>.jbp_patch`).
- `Blueprints/**/*.jbp` (new blueprints) and `*.jbp_patch`. Patch fields: `FieldOverrides` take
  dotted paths, `ComponentsPatches` add components, `ArrayPatches` edit reference arrays
  (`Code.GameCore.Blueprints.BlueprintPatcher`).
- `Localization/enGB.json` as `{"strings": [{"Key", "Value"}]}`.
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
