using System.Collections.Generic;
using IAYBLeaderboard.Steam;
using UnityEngine;
using UnityEngine.UI;

namespace IAYBLeaderboard.UI
{
    public class WorldPanel
    {
        public static WorldPanel Instance { get; private set; }

        private GameObject _root;
        private RectTransform _panelRect;
        private GameObject _contentParent;
        private GameObject _emptyStateObj;
        private TMPro.TextMeshProUGUI _emptyText;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        private string _currentCategory;
        private int _currentLevelId;
        private LevelInformation _currentLevelInfo;
        private string _currentWorldCacheKey;

        private static readonly Vector2 LevelSelectPos = new Vector2(-25.5f, -96f - UIHelper.PanelHeight - 6f);
        private static readonly Vector2 LevelCompletePos = new Vector2(-25.5f, -321f - UIHelper.PanelHeight - 6f);
        private const float SeparatorHeight = 10f;

        public WorldPanel()
        {
            Instance = this;
            UIHelper.EnsureAssetsLoaded();
            CreateUI();
        }

        private void CreateUI()
        {
            var canvas = UIHelper.CreatePanelCanvas("WorldLeaderboardCanvas");
            _root = canvas.gameObject;

            var panelObj = UIHelper.CreatePanelObject(_root.transform, LevelSelectPos);
            _panelRect = panelObj.GetComponent<RectTransform>();

            UIHelper.CreateHeader(panelObj.transform, "WORLD");

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

        public void Show(LevelInformation levelInfo, string category, int levelId, bool isLevelComplete)
        {
            SetLevel(levelInfo, category, levelId);
            _panelRect.anchoredPosition = isLevelComplete ? LevelCompletePos : LevelSelectPos;
            _root.SetActive(true);
            ShowLoading();
            Mod.Instance.LeaderboardManager.DownloadWorldScores(category, levelId, GradeHelper.IsHorde(levelInfo), OnScoresReceived);
        }

        public void RefreshForLevel(LevelInformation levelInfo, string category, int levelId)
        {
            SetLevel(levelInfo, category, levelId);
            if (!_root.activeSelf) _root.SetActive(true);
            ShowLoading();
            Mod.Instance.LeaderboardManager.DownloadWorldScores(category, levelId, GradeHelper.IsHorde(levelInfo), OnScoresReceived);
        }

        public void Hide()
        {
            _root.SetActive(false);
            _currentLevelInfo = null;
            _currentCategory = null;
            _currentLevelId = -1;
            _currentWorldCacheKey = null;
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
            _currentWorldCacheKey = $"world_{category}_{levelId}";
        }

        public bool IsVisible => _root != null && _root.activeSelf;
        public string CurrentCategory => _currentCategory;
        public int CurrentLevelId => _currentLevelId;
        public LevelInformation CurrentLevelInfo => _currentLevelInfo;
        public string CurrentWorldCacheKey => _currentWorldCacheKey;

        private void OnScoresReceived(List<FriendScore> scores)
        {
            ClearRows();

            if (scores == null || scores.Count == 0)
            {
                _emptyStateObj.SetActive(true);
                _emptyText.text = "No global scores yet";
                return;
            }

            _emptyStateObj.SetActive(false);

            int rowCount = 0;
            for (int i = 0; i < scores.Count && rowCount < UIHelper.MaxRows; i++)
            {
                if (scores[i].Rank == -1)
                    CreateSeparator();
                else
                {
                    CreateRow(scores[i]);
                    rowCount++;
                }
            }
        }

        private void CreateSeparator()
        {
            var sep = new GameObject("Separator");
            sep.transform.SetParent(_contentParent.transform, false);
            _rowObjects.Add(sep);
            sep.AddComponent<RectTransform>();
            var sepLayout = sep.AddComponent<LayoutElement>();
            sepLayout.preferredHeight = SeparatorHeight;
            sepLayout.flexibleHeight = 0;
            sepLayout.minHeight = SeparatorHeight;

            var dots = new GameObject("Dots");
            dots.transform.SetParent(sep.transform, false);
            var dotsRect = dots.AddComponent<RectTransform>();
            dotsRect.anchorMin = Vector2.zero;
            dotsRect.anchorMax = Vector2.one;
            dotsRect.offsetMin = Vector2.zero;
            dotsRect.offsetMax = Vector2.zero;
            UIHelper.CreateTMPText(dots, "...", 8, new Color(0.4f, 0.4f, 0.4f),
                TMPro.TextAlignmentOptions.Center);
        }

        private void CreateRow(FriendScore score)
        {
            var row = new GameObject($"Row_{score.Rank}");
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
            rowHLayout.spacing = 4;
            rowHLayout.childAlignment = TextAnchor.MiddleLeft;
            rowHLayout.childForceExpandWidth = false;

            if (score.IsLocalPlayer && score.Rank == 1)
                rowBg.color = new Color(0.5f, 0.55f, 0.45f, 0.2f);
            else if (score.IsLocalPlayer)
                rowBg.color = UIHelper.BlueTransparent;
            else if (score.Rank == 1)
                rowBg.color = UIHelper.GoldTransparent;
            else
                rowBg.color = Color.clear;

            CreateRankBadge(row.transform, score.Rank);

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
            UIHelper.CreateTMPText(nameObj, score.IsLocalPlayer ? "You" : score.PlayerName, 12, Color.white,
                TMPro.TextAlignmentOptions.MidlineLeft, TMPro.FontStyles.Bold);
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1;

            string scoreText = IsCurrentHorde
                ? score.ScoreCentiseconds.ToString()
                : score.TimeSeconds.ToString("F2");

            var timeObj = new GameObject("Score");
            timeObj.transform.SetParent(row.transform, false);
            UIHelper.CreateTMPText(timeObj, scoreText, 12, Color.white,
                TMPro.TextAlignmentOptions.MidlineRight, TMPro.FontStyles.Bold);
            timeObj.AddComponent<LayoutElement>().preferredWidth = 50;
        }

        private void CreateRankBadge(Transform parent, int rank)
        {
            var badge = new GameObject("RankBadge");
            badge.transform.SetParent(parent, false);
            badge.AddComponent<LayoutElement>().preferredWidth = 28;
            badge.GetComponent<LayoutElement>().preferredHeight = 20;

            var borderImg = badge.AddComponent<Image>();
            borderImg.color = UIHelper.BorderWhite;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(badge.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1, 1);
            innerRect.offsetMax = new Vector2(-1, -1);
            inner.AddComponent<Image>().color = UIHelper.BadgeBg;

            var label = new GameObject("Label");
            label.transform.SetParent(badge.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Color rankColor = rank == 1 ? UIHelper.GoldColor : Color.white;
            UIHelper.CreateTMPText(label, "#" + rank, 9, rankColor,
                TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
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
