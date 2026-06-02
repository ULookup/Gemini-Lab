# MCP Disabled Backup

This folder temporarily stores the 4 Unity MCP packages that were moved out of `Packages/` on `2026-06-02`.

Restore steps:
1. Move these 4 folders back into `Packages/`.
2. Re-add the 4 `com.ivanmurzak.unity.mcp*` dependencies to `Packages/manifest.json`.
3. Let Unity refresh `Packages/packages-lock.json` automatically.

`Packages/SkillsForUnity` is intentionally not part of this backup and should remain in `Packages/`.
