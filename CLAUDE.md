# CLAUDE.md

## Project Overview

MelonLoader mod for "I Am Your Beast" (Unity Mono game by Strange Scaffold). Adds Steam-based friends and world leaderboards to the level select and level complete screens.

## Build

```bash
dotnet build -c Release
```

Output goes to the game's `Mods/` folder (configured via `GameDir` property in the csproj). The game path defaults to `C:\Program Files (x86)\Steam\steamapps\common\I Am Your Beast`.

## Architecture

- **Mod.cs** — MelonMod entry point. Lazy-creates panels, pumps `SteamAPI.RunCallbacks()` and the operation queue in `OnUpdate`, hides panels on scene transitions.
- **Steam/SteamLeaderboardManager.cs** — Serialized async operation queue for Steamworks API calls (find/create boards, upload scores, download friends/world entries). Caches board handles and downloaded scores. Operations are deferred to `ProcessQueue()` called from `OnUpdate` to avoid CallResult re-entrancy.
- **UI/UIHelper.cs** — Shared static utilities: colors, layout constants, game asset loading (font + UI_MilitarySquare sprite, loaded once), and factory methods for TMP text, grade badges, panel canvas/header/content scaffolding.
- **UI/LeaderboardPanel.cs** — Friends leaderboard panel. Shows up to 8 friend entries with grade badges.
- **UI/WorldPanel.cs** — World leaderboard panel. Shows #1 globally, separator, then players around current user with rank badges.
- **UI/GradeHelper.cs** — Computes grade index from time (timed levels) or score (horde levels) using game's own thresholds.
- **Patches/InitPatches.cs** — `GameManager.Initialize` postfix. Bulk-uploads all existing save scores after Steam is initialized.
- **Patches/LevelSelectPatches.cs** — `UILevelSelectFeature.Refresh` + `UILevelSelectionRoot.SelectCategory` + `UILevelSelectionRoot.OpenCategoryList` postfixes. Shows/hides/refreshes panels.
- **Patches/LevelCompletePatches.cs** — `UILevelCompleteScreen.DisplayMenu` postfix. Uploads score and shows panels.

## Key Technical Details

- **Game bug:** `LevelInformation.GetBestRankTime()` returns `minimumCompleteTime` instead of `bestRankTime`. Use `GetTimeForGrade()` which reads the correct private field.
- **Steam callbacks:** The game never calls `SteamAPI.RunCallbacks()`. The mod must do it in `OnUpdate`.
- **Queue re-entrancy:** `FinishOperation()` must NOT call `TryProcessNext()` directly — that would call `_downloadResult.Set()` from inside a Steam callback, which silently fails. Queue advances via `ProcessQueue()` from `OnUpdate`.
- **Duplicate downloads:** `UILevelSelectFeature.Refresh()` postfix fires 2-3 times per level selection. The manager tracks `_pendingDownloadKey` / `_pendingWorldKey` to skip duplicates.
- **Target framework:** net461 (game is Mono but assemblies reference .NET 4.x types).

## Conventions

- All positions specified in 1080p reference coordinates (CanvasScaler handles scaling). User's specs were at 1440p, divided by 1.333.
- Leaderboard names: `iayb_friends_{category}_{levelId}` for timed, `iayb_horde_{category}_{levelId}` for horde.
- Timed scores stored as centiseconds (int). Horde scores stored as raw ints.
- Grade index 0-4 maps to D/C/B/A/S.

## Changelog Flow

Keep `CHANGELOG.md` updated with an `[Unreleased]` section at the top. When tagging a release (e.g. `v1.0.0`), the GitHub Actions workflow extracts that version's notes and uses them as the release body. See `.github/workflows/release.yml`.

## Release Flow

1. Update `CHANGELOG.md` — move items from `[Unreleased]` to a new version section
2. Update version in `Mod.cs` (`MelonInfo` attribute)
3. Build: `dotnet build -c Release`
4. Copy DLL to repo root: `cp "$(GameDir)/Mods/IAYBLeaderboard.dll" .`
5. Commit: `release: v1.x.x`
6. Tag and push: `git tag v1.x.x && git push origin master --tags`
7. GitHub Actions creates a release with changelog notes and the DLL attached automatically
