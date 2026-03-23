using System.Collections.Generic;
using IAYBLeaderboard.Steam;
using UnityEngine;
using UnityEngine.UI;

namespace IAYBLeaderboard.UI
{
    public class LeaderboardPanel
    {
        public static LeaderboardPanel Instance { get; private set; }

        private GameObject _root;
        private RectTransform _panelRect;
        private GameObject _contentParent;
        private GameObject _emptyStateObj;
        private TMPro.TextMeshProUGUI _emptyText;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        private string _currentCategory;
        private int _currentLevelId;
        private LevelInformation _currentLevelInfo;
        private string _currentCacheKey;

        private static readonly Vector2 LevelSelectPos = new Vector2(-25.5f, -96f);
        private static readonly Vector2 LevelCompletePos = new Vector2(-25.5f, -321f);

        public LeaderboardPanel()
        {
            Instance = this;
            UIHelper.EnsureAssetsLoaded();
            CreateUI();
        }

        private void CreateUI()
        {
            var canvas = UIHelper.CreatePanelCanvas("FriendsLeaderboardCanvas");
            _root = canvas.gameObject;

            var panelObj = UIHelper.CreatePanelObject(_root.transform, LevelSelectPos);
            _panelRect = panelObj.GetComponent<RectTransform>();

            UIHelper.CreateHeader(panelObj.transform, "FRIENDS");

            _contentParent = UIHelper.CreateContentParent(panelObj.transform);

            _emptyStateObj = new GameObject("EmptyState");
            _emptyStateObj.transform.SetParent(_contentParent.transform, false);
            _emptyStateObj.AddComponent<RectTransform>();
            var emptyLayout = _emptyStateObj.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 40;
            emptyLayout.flexibleHeight = 0;
            _emptyText = UIHelper.CreateTMPText(_emptyStateObj, "", 12,
                new Color(0.5f, 0.5f, 0.5f), TMPro.TextAlignmentOptions.Center);

            Hide();
        }

        private bool IsCurrentHorde => GradeHelper.IsHorde(_currentLevelInfo);

        public void Show(LevelInformation levelInfo, string category, int levelId)
        {
            SetLevel(levelInfo, category, levelId);
            _panelRect.anchoredPosition = LevelSelectPos;
            _root.SetActive(true);
            ShowLoading();
            Mod.Instance.LeaderboardManager.DownloadFriendScores(category, levelId, GradeHelper.IsHorde(levelInfo), OnScoresReceived);
        }

        public void RefreshForLevel(LevelInformation levelInfo, string category, int levelId)
        {
            SetLevel(levelInfo, category, levelId);
            if (!_root.activeSelf) _root.SetActive(true);
            ShowLoading();
            Mod.Instance.LeaderboardManager.DownloadFriendScores(category, levelId, GradeHelper.IsHorde(levelInfo), OnScoresReceived);
        }

        public void ShowForLevelComplete(LevelInformation levelInfo, string category, int levelId)
        {
            SetLevel(levelInfo, category, levelId);
            _panelRect.anchoredPosition = LevelCompletePos;
            _root.SetActive(true);
            ShowLoading();
            Mod.Instance.LeaderboardManager.DownloadFriendScores(category, levelId, GradeHelper.IsHorde(levelInfo), OnScoresReceived);
        }

        public void Hide()
        {
            _root.SetActive(false);
            _currentLevelInfo = null;
            _currentCategory = null;
            _currentLevelId = -1;
            _currentCacheKey = null;
        }

        public void ShowEmptyState(string message)
        {
            ClearRows();
            _emptyStateObj.SetActive(true);
            _emptyText.text = message;
            _root.SetActive(true);
        }

        private void ShowLoading()
        {
            ClearRows();
            _emptyStateObj.SetActive(true);
            _emptyText.text = "...";
        }

        private void SetLevel(LevelInformation levelInfo, string category, int levelId)
        {
            _currentLevelInfo = levelInfo;
            _currentCategory = category;
            _currentLevelId = levelId;
            _currentCacheKey = $"{category}_{levelId}";
        }

        public bool IsVisible => _root != null && _root.activeSelf;
        public string CurrentCategory => _currentCategory;
        public int CurrentLevelId => _currentLevelId;
        public LevelInformation CurrentLevelInfo => _currentLevelInfo;
        public string CurrentCacheKey => _currentCacheKey;

        private void OnScoresReceived(List<FriendScore> scores)
        {
            ClearRows();

            if (scores == null || scores.Count == 0)
            {
                _emptyStateObj.SetActive(true);
                _emptyText.text = "No friends have played this level";
                return;
            }

            _emptyStateObj.SetActive(false);

            if (IsCurrentHorde)
                scores.Sort((a, b) => b.ScoreCentiseconds.CompareTo(a.ScoreCentiseconds));
            else
                scores.Sort((a, b) => a.ScoreCentiseconds.CompareTo(b.ScoreCentiseconds));

            for (int i = 0; i < scores.Count && i < UIHelper.MaxRows; i++)
                CreateRow(scores[i], i);
        }

        private void CreateRow(FriendScore score, int index)
        {
            var row = new GameObject($"Row_{index}");
            row.transform.SetParent(_contentParent.transform, false);
            _rowObjects.Add(row);

            row.AddComponent<RectTransform>();
            var rowBg = row.AddComponent<Image>();
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = UIHelper.RowHeight;
            rowLayout.flexibleHeight = 0;
            rowLayout.minHeight = UIHelper.RowHeight;

            var rowHLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowHLayout.padding = new RectOffset(6, 8, 3, 3);
            rowHLayout.spacing = 6;
            rowHLayout.childAlignment = TextAnchor.MiddleLeft;
            rowHLayout.childForceExpandWidth = false;

            if (score.IsLocalPlayer && index == 0)
                rowBg.color = new Color(0.5f, 0.55f, 0.45f, 0.2f);
            else if (score.IsLocalPlayer)
                rowBg.color = UIHelper.BlueTransparent;
            else if (index == 0)
                rowBg.color = UIHelper.GoldTransparent;
            else
                rowBg.color = Color.clear;

            int gradeIdx = 0;
            string gradeLetter = "?";
            if (_currentLevelInfo != null)
            {
                gradeIdx = IsCurrentHorde
                    ? GradeHelper.GetHordeGradeIndex(_currentLevelInfo, score.ScoreCentiseconds)
                    : GradeHelper.GetGradeIndex(_currentLevelInfo, score.TimeSeconds);
                gradeLetter = GradeHelper.GetGradeLetter(gradeIdx);
            }

            UIHelper.CreateGradeBadge(row.transform, gradeLetter, gradeIdx);

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            UIHelper.CreateTMPText(nameObj, score.IsLocalPlayer ? "You" : score.PlayerName, 13, Color.white,
                TMPro.TextAlignmentOptions.MidlineLeft, TMPro.FontStyles.Bold);
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1;

            string scoreText = IsCurrentHorde
                ? score.ScoreCentiseconds.ToString()
                : score.TimeSeconds.ToString("F2");

            var timeObj = new GameObject("Score");
            timeObj.transform.SetParent(row.transform, false);
            UIHelper.CreateTMPText(timeObj, scoreText, 13, Color.white,
                TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold);
            timeObj.AddComponent<LayoutElement>().preferredWidth = 55;
        }

        private void ClearRows()
        {
            foreach (var row in _rowObjects)
                Object.Destroy(row);
            _rowObjects.Clear();
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
        }
    }
}
