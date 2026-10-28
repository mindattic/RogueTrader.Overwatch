---
codex: 1
project: RogueTrader.Overwatch
code: RTO
layer: stories
status: living
updated: 2026-10-26
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# RogueTrader.Overwatch — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| RTO-T1 | 2026-10-26 | docs | closed | Start the permanent ticket log in the ticket database | — |
| RTO-T2 | 2026-10-26 | feature | closed | Create the RogueTrader.Overwatch repo: Overwatch abilities on every ranged weapon for heroes and enemies (Reaction Shot, Overwatch, Suppression) | 3d014c5 |
| RTO-T3 | 2026-10-26 | chore | closed | Build the mod without Unity: script that assembles the Owlcat mod folder (manifest, settings, blueprints, patches, enGB) and installs/enables it | 5873bbc |
| RTO-T4 | 2026-10-26 | feature | closed | Give every unit holding a ranged weapon the Overwatch buttons (AddAbilitiesToCurrentWeapon on CommonMobFact for enemies, hero units for the party) | Confirmed in game by the user, 2026-10-26 |
| RTO-T5 | 2026-10-26 | feature | closed | Reflex Shot: XCOM 2 style, fire once at the first enemy that moves or acts within range, then end | Confirmed in game by the user, 2026-10-26 |
| RTO-T6 | 2026-10-26 | feature | closed | Overwatch: draggable cone, one shot per AP left when cast; spreads fire across enemies, re-shoots one only after it moves 2+ tiles | Confirmed in game by the user, 2026-10-26 |
| RTO-T7 | 2026-10-26 | feature | wont-do | Kill Zone Overwatch: aimed wide cone, shoot every enemy that moves in it once until your next turn | Merged into RTO-T6 Overwatch (draggable cone, shots = remaining AP) per user 2026-10-26 |
| RTO-T8 | 2026-10-26 | feature | closed | Suppression: pin one enemy; it takes a ranged attack of opportunity if it moves, with cosmetic always-miss fire during its turn | Confirmed in game by the user, 2026-10-26 |
| RTO-T9 | 2026-10-26 | feature | wont-do | Lane Overwatch: long narrow line aimed down a corridor, one shot at the first enemy in it | User dropped Lane Overwatch on 2026-10-26 |
| RTO-T10 | 2026-10-26 | feature | closed | Overwatch icons: placeholder game sprites per type now, native-looking final art later | Confirmed in game by the user, 2026-10-26 |
| RTO-T11 | 2026-10-26 | feature | closed | Enemy AI uses Overwatch via BlueprintBrain (when no target, spare AP, ranged role) | Confirmed in game by the user, 2026-10-26 |
| RTO-T12 | 2026-10-26 | feature | wont-do | Drag-to-size Overwatch cone (Gears Tactics style) via C#, after the data-only types are tested | Merged into RTO-T6 Overwatch (draggable cone is part of it) per user 2026-10-26 |
| RTO-T13 | 2026-10-26 | fix | closed | Overwatch shot count is lost on save/load: an active Overwatch reloads with 1 shot instead of the AP-based count | 542091f |
| RTO-T14 | 2026-10-26 | chore | closed | Rename Reaction Shot to Reflex Shot (in-game name, files, docs) | 7afa83f |
| RTO-T15 | 2026-10-26 | feature | closed | Release 1.0.0: dist zip of the Owlcat mod folder, built by dotnet build -c Release | dist/RogueTrader.Overwatch-1.0.0.zip, built by dotnet build -c Release; confirmed in game |
| RTO-T16 | 2026-10-26 | docs | open | Nexus Mods page: header, logo, summary and BBCode description in docs/NEXUS_PAGE.md | — |
| RTO-T17 | 2026-10-26 | fix | closed | Ship only the hotbar icons in the mod, not the Nexus header and logo | 0fe4406 |
| RTO-T18 | 2026-10-26 | feature | closed | Put Reflex Shot, Overwatch and Suppression on every party member's ability bar (last 3 slots, else later rows), greyed out without a ranged weapon in the active set | Confirmed in game by the user, 2026-10-26 |
| RTO-T19 | 2026-10-26 | docs | closed | In-game validation guide (docs/TESTING.md) covering every rule and equip situation, with README, DESIGN and Nexus page brought up to date | ffca20e |
| RTO-T20 | 2026-10-26 | chore | closed | Build on RogueTrader.Shared: game paths, references, packaging, sprite loading and logging from the shared source | d7e8fc7 |
| RTO-T21 | 2026-10-26 | feature | closed | Reflex Shot: pick an expected facing; aim bonus for targets ahead, penalty growing with the turn needed, largest at 180 degrees | Confirmed in game by the user, 2026-10-26 |
| RTO-T22 | 2026-10-26 | fix | closed | The game skips all the mod's blueprints: the install and the release zip have no Bundles folder, and LoadBundles throws before LoadBlueprints | bdb572b |
| RTO-T23 | 2026-10-26 | fix | closed | Overwatch cone starts at the clicked cell instead of the shooter: the resize patch passed directional=false | cc99f89 |
| RTO-T24 | 2026-10-26 | fix | closed | Overwatch never re-shoots anyone while a downed (unconscious) enemy is in the cone: it counted as not yet shot | b61773e |
| RTO-T25 | 2026-10-26 | fix | closed | Companions recruited or mercenaries hired mid-area get no Overwatch buttons until the next area load | 9bbc68e |
| RTO-T26 | 2026-10-26 | fix | closed | Suppression: a cancelled or blocked move leaves the target marked, so a later push or dodge draws a shot | 3abbc03 |
| RTO-T27 | 2026-10-26 | feature | closed | Suppression fires when the target leaves its square by any means (walk, charge, jump, push), once per command | 3abbc03 |
| RTO-T28 | 2026-10-26 | fix | closed | Reflex Shot's aim modifier leaks into a later Suppression shot by the same shooter at the same target (cleared only by the next overwatch cast) | 0d9c1c5 |
| RTO-T29 | 2026-10-26 | fix | closed | Suppression: a push draws a shot for every square crossed instead of one, because the target has no command of its own | e944409 |
| RTO-T30 | 2026-10-26 | fix | closed | Pets get the party Overwatch feature and three always-greyed buttons on their ability bar | 9bb7706 |
| RTO-T31 | 2026-10-26 | fix | closed | Enemies behind cover may never cast Reflex Shot: the facing point one cell away is often the cover itself and fails CanTarget | 395034e |
| RTO-T32 | 2026-10-26 | fix | closed | Reflex Shot's aim modifier can be lost before the hit roll: the attack command may end before RuleCalculateHitChances runs | 0d9c1c5 |
| RTO-T33 | 2026-10-26 | fix | closed | TESTING.md results table lacks rows for the new cases, and DESIGN says the party feature goes to every character (pets are skipped) | 4a893da |
| RTO-T34 | 2026-10-26 | fix | closed | Game hangs on the first loading screen: NullReferenceException in OwlcatModification.TryLoadBundleAsync while loading a scene bundle | 5600c29 |
| RTO-T35 | 2026-10-26 | fix | closed | Loading a save stops the game: the CommonMobFact component patch writes m_Flags as a number, which the patcher's strict enum reader rejects; blueprints also use an invalid Author | 138a2e9 |
| RTO-T36 | 2026-10-26 | docs | closed | Say that pushes trigger all three abilities: in-game descriptions, DESIGN and TESTING cases D2c and E6a | eeae417 |
| RTO-T37 | 2026-10-27 | chore | closed | Standardize phrasing across the English ability/buff descriptions as the stable source text for localization | b92b68a |
| RTO-T38 | 2026-10-27 | feature | closed | French (frFR) localization for Reflex Shot, Overwatch and Suppression | frFR |
| RTO-T39 | 2026-10-27 | feature | closed | German (deDE) localization for Reflex Shot, Overwatch and Suppression | deDE |
| RTO-T40 | 2026-10-27 | feature | closed | Spanish - Spain (esES) localization for Reflex Shot, Overwatch and Suppression | esES |
| RTO-T41 | 2026-10-27 | feature | closed | Russian (ruRU) localization for Reflex Shot, Overwatch and Suppression | ruRU |
| RTO-T42 | 2026-10-27 | feature | open | Simplified Chinese (zhCN) localization for Reflex Shot, Overwatch and Suppression | — |
| RTO-T43 | 2026-10-27 | feature | open | Japanese (jaJP) localization for Reflex Shot, Overwatch and Suppression | — |
| RTO-T44 | 2026-10-27 | feature | open | Turkish (trTR) localization for Reflex Shot, Overwatch and Suppression | — |
