# NuGet Disabled Backup

This folder temporarily stores `Assets/Plugins/NuGet` and its `.meta` file after they were moved out of the active Unity asset path on `2026-06-02`.

Restore steps:
1. Move `NuGet/` back to `Assets/Plugins/NuGet`.
2. Move `NuGet.meta` back to `Assets/Plugins/NuGet.meta`.
3. If needed, restore `Standalone: UNITY_MCP_READY` in `ProjectSettings/ProjectSettings.asset`.

`Packages/SkillsForUnity` is unrelated and should remain untouched.
