using HarmonyLib;
using MelonLoader;
using Progress;
using Steamworks;
using IAYBLeaderboard.UI;

namespace IAYBLeaderboard.Patches
{
    [HarmonyPatch(typeof(GameManager), "Initialize")]
    public static class InitPatches
    {
        [HarmonyPostfix]
        public static void Postfix(GameManager __instance)
        {
            if (__instance != GameManager.instance)
                return;

            if (GameManager.instance.IsDRMFree())
                return;

            try
            {
                if (!SteamAPI.IsSteamRunning())
                    return;
            }
            catch
            {
                return;
            }

            var logger = Mod.Instance.LoggerInstance;
            logger.Msg("Bulk uploading existing scores to Steam leaderboards...");

            var progressManager = GameManager.instance.progressManager;
            var collections = progressManager.GetAllCollections();
            int uploadCount = 0;

            foreach (var collection in collections)
            {
                var levels = collection.GetAllLevels();
                foreach (var levelInfo in levels)
                {
                    if (levelInfo == null || levelInfo.GetLevelNumber() < 0)
                        continue;
                    if (!GradeHelper.IsGameplayLevel(levelInfo))
                        continue;

                    var levelData = progressManager.GetLevelData(levelInfo);
                    if (levelData == null || !levelData.GetLevelCompleted())
                        continue;

                    string category = collection.GetSaveName();
                    int levelId = levelInfo.GetLevelNumber();

                    if (GradeHelper.IsHorde(levelInfo))
                    {
                        int hordeScore = levelData.GetHordeScore();
                        if (hordeScore > 0)
                        {
                            Mod.Instance.LeaderboardManager.UploadHordeScore(category, levelId, hordeScore);
                            uploadCount++;
                        }
                    }
                    else
                    {
                        float bestTime = levelData.GetBestTime();
                        if (bestTime > 0f && bestTime < 999f)
                        {
                            Mod.Instance.LeaderboardManager.UploadScore(category, levelId, bestTime);
                            uploadCount++;
                        }
                    }
                }
            }

            logger.Msg($"Queued {uploadCount} score uploads.");
        }
    }
}
