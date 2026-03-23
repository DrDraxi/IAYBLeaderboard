using HarmonyLib;
using MelonLoader;
using Progress;
using IAYBLeaderboard.UI;

namespace IAYBLeaderboard.Patches
{
    [HarmonyPatch(typeof(UILevelSelectFeature), "Refresh")]
    public static class LevelSelectFeatureRefreshPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UILevelSelectFeature __instance, SceneInformation info)
        {
            var panel = Mod.Instance?.Panel;
            var world = Mod.Instance?.WorldPanel;
            if (panel == null)
                return;

            try
            {
                if (!Steamworks.SteamAPI.IsSteamRunning())
                {
                    panel.Hide();
                    world?.Hide();
                    return;
                }
            }
            catch
            {
                panel.Hide();
                world?.Hide();
                return;
            }

            if (info == null || !(info is LevelInformation levelInfo))
            {
                panel.Hide();
                world?.Hide();
                return;
            }

            if (!GradeHelper.IsGameplayLevel(levelInfo))
            {
                panel.Hide();
                world?.Hide();
                return;
            }

            if (levelInfo.GetLevelNumber() < 0)
            {
                panel.Hide();
                world?.Hide();
                return;
            }

            string category = levelInfo.GetLevelCategoryName();
            int levelId = levelInfo.GetLevelNumber();

            var levelData = GameManager.instance.progressManager.GetLevelData(levelInfo);
            if (levelData == null || !levelData.GetLevelCompleted())
            {
                panel.ShowEmptyState("Complete this level to see rankings");
                world?.ShowEmptyState("");
                return;
            }

            panel.Show(levelInfo, category, levelId);
            world?.Show(levelInfo, category, levelId, false);
        }
    }

    [HarmonyPatch(typeof(UILevelSelectionRoot), "SelectCategory")]
    public static class LevelSelectCategoryPatch
    {
        [HarmonyPostfix]
        public static void Postfix(UILevelSelectionRoot __instance)
        {
            var panel = Mod.Instance?.Panel;
            var world = Mod.Instance?.WorldPanel;
            if (panel == null)
                return;

            try
            {
                if (!Steamworks.SteamAPI.IsSteamRunning())
                    return;
            }
            catch
            {
                return;
            }

            var featured = __instance.GetFeatured();
            if (featured is LevelInformation levelInfo &&
                GradeHelper.IsGameplayLevel(levelInfo) &&
                levelInfo.GetLevelNumber() >= 0)
            {
                var levelData = GameManager.instance.progressManager.GetLevelData(levelInfo);
                if (levelData != null && levelData.GetLevelCompleted())
                {
                    string category = levelInfo.GetLevelCategoryName();
                    int levelId = levelInfo.GetLevelNumber();
                    panel.RefreshForLevel(levelInfo, category, levelId);
                    world?.RefreshForLevel(levelInfo, category, levelId);
                }
                else
                {
                    panel.ShowEmptyState("Complete this level to see rankings");
                    world?.ShowEmptyState("");
                }
            }
        }
    }

    [HarmonyPatch(typeof(UILevelSelectionRoot), "OpenCategoryList")]
    public static class LevelSelectOpenCategoryPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Mod.Instance?.Panel?.Hide();
            Mod.Instance?.WorldPanel?.Hide();
        }
    }
}
