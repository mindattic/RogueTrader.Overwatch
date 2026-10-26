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
| RTO-T4 | 2026-10-26 | feature | open | Give every unit holding a ranged weapon the Overwatch buttons (AddAbilitiesToCurrentWeapon on CommonMobFact for enemies, hero units for the party) | — |
| RTO-T5 | 2026-10-26 | feature | open | Reflex Shot: XCOM 2 style, fire once at the first enemy that moves or acts within range, then end | — |
| RTO-T6 | 2026-10-26 | feature | open | Overwatch: draggable cone, one shot per AP left when cast; spreads fire across enemies, re-shoots one only after it moves 2+ tiles | — |
| RTO-T7 | 2026-10-26 | feature | wont-do | Kill Zone Overwatch: aimed wide cone, shoot every enemy that moves in it once until your next turn | Merged into RTO-T6 Overwatch (draggable cone, shots = remaining AP) per user 2026-10-26 |
| RTO-T8 | 2026-10-26 | feature | open | Suppression: pin one enemy; it takes a ranged attack of opportunity if it moves, with cosmetic always-miss fire during its turn | — |
| RTO-T9 | 2026-10-26 | feature | wont-do | Lane Overwatch: long narrow line aimed down a corridor, one shot at the first enemy in it | User dropped Lane Overwatch on 2026-10-26 |
| RTO-T10 | 2026-10-26 | feature | open | Overwatch icons: placeholder game sprites per type now, native-looking final art later | — |
| RTO-T11 | 2026-10-26 | feature | open | Enemy AI uses Overwatch via BlueprintBrain (when no target, spare AP, ranged role) | — |
| RTO-T12 | 2026-10-26 | feature | wont-do | Drag-to-size Overwatch cone (Gears Tactics style) via C#, after the data-only types are tested | Merged into RTO-T6 Overwatch (draggable cone is part of it) per user 2026-10-26 |
| RTO-T13 | 2026-10-26 | fix | closed | Overwatch shot count is lost on save/load: an active Overwatch reloads with 1 shot instead of the AP-based count | 542091f |
| RTO-T14 | 2026-10-26 | chore | closed | Rename Reaction Shot to Reflex Shot (in-game name, files, docs) | 7afa83f |
| RTO-T15 | 2026-10-26 | feature | open | Release 1.0.0: dist zip of the Owlcat mod folder, built by dotnet build -c Release | — |
| RTO-T16 | 2026-10-26 | docs | open | Nexus Mods page: header, logo, summary and BBCode description in docs/NEXUS_PAGE.md | — |
| RTO-T17 | 2026-10-26 | fix | closed | Ship only the hotbar icons in the mod, not the Nexus header and logo | 0fe4406 |
| RTO-T18 | 2026-10-26 | feature | open | Put Reflex Shot, Overwatch and Suppression on every party member's ability bar (last 3 slots, else later rows), greyed out without a ranged weapon in the active set | — |
| RTO-T19 | 2026-10-26 | docs | closed | In-game validation guide (docs/TESTING.md) covering every rule and equip situation, with README, DESIGN and Nexus page brought up to date | ffca20e |
| RTO-T20 | 2026-10-26 | chore | open | Build on RogueTrader.Shared: game paths, references, packaging, sprite loading and logging from the shared source | — |
