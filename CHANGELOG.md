# 📜 Changelog

All notable changes to this project will be documented in this file.

## [v0.3.0] - 2025-04-01
### Added
- New reset system (`reset_player_data`, `remove_building`) via updated Dojo bindings.
- Upgraded contract interfaces with more modular Dojo components.
- Full support for `city-builder-v2` world.

### Changed
- Major code refactor: moved game logic to `DojoProject/bindings/unity/Contracts`.
- Updated world seed and project metadata in `dojo_dev.toml` and `manifest_dev.json`.

### Removed
- Legacy MonoBehaviours: `TileManager`, `TileVisual`, `GameStateManager`.
- Obsolete interop files and static libraries (`libdojo_c.a`, `JsonRpcClient.cs`).

---

## [v0.2.0] - 2025-03-21
### Added
- `reset_player_data` and `remove_building` systems.
- Modular design for Dojo-based ECS integration.

### Changed
- Updated RPC URL and account key.
- Transitioned to new contract addresses for Player, Tile, and Building systems.

### Removed
- Legacy client bindings and outdated C# infrastructure.

---

## [v0.1.0] - 2025-02-04
### Initial Release
- Basic tile selection and ownership system.
- On-chain player initialization and simple transaction flow.
- Unity + Dojo integration for proof-of-concept gameplay.
