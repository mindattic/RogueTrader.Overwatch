# MindAttic project agent entrypoint

Read the shared protocol at ..\mindattic-agent-standard\AGENTS.md and this project's own
documentation. Common prompt commands are implemented by the shared runner; do not add a second
copy under a provider-specific command folder.

## Project context

RogueTrader.Overwatch adds Overwatch abilities to Warhammer 40,000: Rogue Trader: Reaction Shot,
Overwatch (draggable cone) and Suppression, on every ranged weapon's action bar for heroes and
enemies alike. It builds on the engine's own overwatch mechanic. `docs/DESIGN.md` holds the rules
and the engine findings; each ability's ticket (`tickets ticket show RTO-T<n>`) holds the details.

- Game install: `D:\Games\Steam\steamapps\common\Warhammer 40,000 Rogue Trader`
- Game code: `WH40KRT_Data\Managed\Code.dll` (gameplay), `RogueTrader.GameCore.dll` (mod loader,
  blueprint patcher). Decompile a type with `ilspycmd -t <Full.Type.Name> <dll>`.
- Blueprints as JSON: `<game>\Modding\WhRtModificationTemplate.tar` →
  `WhRtModificationTemplate/Blueprints/**/*.jbp` (extract with `tar --force-local`).
- A blueprint whose `Meta.ShadowDeleted` is `true` was cut and isn't in the shipped game; never
  reference one, copy what's needed into this mod.
- The mod is an Owlcat modification (blueprints, patches, localization and a C# assembly in
  `Assemblies/`), built with `dotnet` and no Unity. It installs to
  `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Modifications\` and is
  enabled in `OwlcatModificationManagerSettings.json` next to that folder; Shift+F10 in game opens
  Owlcat's mod manager. Mod loader messages go to `Player.log` (`PFLog.Mods`).
