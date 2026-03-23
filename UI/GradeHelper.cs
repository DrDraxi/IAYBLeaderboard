using UnityEngine;

namespace IAYBLeaderboard.UI
{
    public static class GradeHelper
    {
        private static readonly string[] GradeLetters = { "D", "C", "B", "A", "S" };

        public static int GetGradeIndex(LevelInformation levelInfo, float time)
        {
            for (int g = 4; g >= 0; g--)
            {
                if (time <= levelInfo.GetTimeForGrade(g))
                    return g;
            }
            return 0;
        }

        public static int GetHordeGradeIndex(LevelInformation levelInfo, int score)
        {
            int[] thresholds = levelInfo.GetHordeScoreThresholds();
            if (thresholds == null || thresholds.Length == 0)
                return 0;

            int grade = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (score >= thresholds[i])
                    grade = i + 1;
            }
            return Mathf.Clamp(grade, 0, 4);
        }

        public static string GetGradeLetter(int gradeIndex)
        {
            return GradeLetters[Mathf.Clamp(gradeIndex, 0, 4)];
        }

        public static string GetGradeLetter(LevelInformation levelInfo, float time)
        {
            return GetGradeLetter(GetGradeIndex(levelInfo, time));
        }

        public static bool IsHorde(LevelInformation levelInfo)
        {
            return levelInfo != null && levelInfo.GetLevelType() == LevelInformation.LevelType.Horde;
        }

        public static bool IsGameplayLevel(LevelInformation levelInfo)
        {
            if (levelInfo == null) return false;
            var type = levelInfo.GetLevelType();
            return type == LevelInformation.LevelType.Timed || type == LevelInformation.LevelType.Horde;
        }
    }
}
