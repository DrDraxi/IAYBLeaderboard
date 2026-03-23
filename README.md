# IAYBLeaderboard

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod for **I Am Your Beast** that adds friends and global leaderboards to the level select and level complete screens.

## Features

- **Leaderboard** — see your Steam friends' best times for each level
- **World Leaderboard** — see the #1 player globally and players ranked around you
- **Grade Badges** — each entry shows a letter grade (D/C/B/A/S) matching the game's grading system
- **Horde Mode** — supports both timed levels (lower time = better) and horde levels (higher score = better)
- **Auto-sync** — uploads your existing save data to Steam leaderboards on game launch
- **Live updates** — uploads your score on level completion and refreshes the panel

## Screenshots

*TODO: Add screenshots*

## Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader) v0.7+ for I Am Your Beast
2. Download `IAYBLeaderboard.dll` from the [latest release](../../releases/latest)
3. Drop it into the game's `Mods/` folder
4. Launch the game

## Building from Source

Requires .NET SDK 7.0+ and the game installed via Steam.

The `.csproj` expects the game at the default Steam location. If yours differs, edit the `GameDir` property in `IAYBLeaderboard.csproj`.

```bash
dotnet build -c Release
```

The built DLL is output directly to the game's `Mods/` folder.

## How It Works

- Uses **Steamworks.NET** (already bundled with the game) to create per-level Steam leaderboards
- Friends panel uses `k_ELeaderboardDataRequestFriends` to fetch friend scores
- World panel chains `k_ELeaderboardDataRequestGlobal` (#1) + `k_ELeaderboardDataRequestGlobalAroundUser` (neighborhood)
- Times stored as centiseconds (int), horde scores as raw ints
- UI built at runtime using Unity's UI system with TextMeshPro and the game's own font
- Three Harmony patches hook into `GameManager.Initialize`, `UILevelSelectFeature.Refresh`, and `UILevelCompleteScreen.DisplayMenu`

## License

MIT
