using HarmonyLib;
using MelonLoader;
using Steamworks;
using IAYBLeaderboard.UI;

namespace IAYBLeaderboard.Patches
{
    [HarmonyPatch(typeof(UILevelCompleteScreen), "DisplayMenu")]
    public static class LevelCompletePatches
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Mod.Instance == null)
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

            var levelController = GameManager.instance.levelController;
            var infoSetter = levelController.GetInformationSetter();
            var levelInfo = infoSetter.GetInformation();

            if (levelInfo == null || levelInfo.GetLevelNumber() < 0)
                return;
            if (!GradeHelper.IsGameplayLevel(levelInfo))
                return;

            string category = levelInfo.GetLevelCategoryName();
            int levelId = levelInfo.GetLevelNumber();
            var logger = Mod.Instance.LoggerInstance;

            if (GradeHelper.IsHorde(levelInfo))
            {
                int hordeScore = levelController.GetHordeManager().GetScore();
                logger.Msg($"Level complete (horde): {category}/{levelId} score={hordeScore} — uploading");
                Mod.Instance.LeaderboardManager.UploadHordeScore(category, levelId, hordeScore);
            }
            else
            {
                float time = levelController.GetCombatTimer().GetTime();
                logger.Msg($"Level complete: {category}/{levelId} in {time:F2}s — uploading");
                Mod.Instance.LeaderboardManager.UploadScore(category, levelId, time);
            }

            Mod.Instance.LeaderboardManager.InvalidateCache(category, levelId);

            Mod.Instance.Panel.ShowForLevelComplete(levelInfo, category, levelId);
            Mod.Instance.WorldPanel.Show(levelInfo, category, levelId, true);
        }
    }
}
