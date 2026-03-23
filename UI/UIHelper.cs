using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IAYBLeaderboard.UI
{
    public static class UIHelper
    {
        public static readonly Color GoldColor = new Color(0.832f, 0.659f, 0.141f, 1f);
        public static readonly Color GoldTransparent = new Color(0.832f, 0.659f, 0.141f, 0.15f);
        public static readonly Color BlueTransparent = new Color(0.3f, 0.5f, 0.8f, 0.2f);
        public static readonly Color PureBg = new Color(0f, 0f, 0f, 1f);
        public static readonly Color BadgeBg = new Color(0f, 0f, 0f, 1f);
        public static readonly Color BorderWhite = new Color(0.7f, 0.7f, 0.7f, 1f);

        public const float PanelWidth = 315f;
        public const float HeaderHeight = 61.5f;
        public const float RowHeight = 30f;
        public const float BottomPadding = 20f;
        public const int MaxRows = 8;
        public static readonly float PanelHeight = HeaderHeight + (RowHeight * MaxRows) + ((MaxRows - 1) * 1f) + BottomPadding;

        public static TMP_FontAsset GameFont { get; private set; }
        public static Sprite BgSprite { get; private set; }
        private static bool _assetsLoaded;

        public static void EnsureAssetsLoaded()
        {
            if (_assetsLoaded) return;
            _assetsLoaded = true;

            var allTmpTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in allTmpTexts)
            {
                if (t.font != null)
                {
                    GameFont = t.font;
                    Mod.Instance.LoggerInstance.Msg($"[UIHelper] Found game font: {GameFont.name}");
                    break;
                }
            }

            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s.name == "UI_MilitarySquare")
                {
                    BgSprite = s;
                    Mod.Instance.LoggerInstance.Msg($"[UIHelper] Found UI_MilitarySquare sprite (border: {s.border})");
                    break;
                }
            }
        }

        public static TextMeshProUGUI CreateTMPText(GameObject obj, string text, float fontSize, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
            FontStyles style = FontStyles.Normal)
        {
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            if (GameFont != null)
                tmp.font = GameFont;
            return tmp;
        }

        public static void CreateGradeBadge(Transform parent, string letter, int gradeIdx)
        {
            var badge = new GameObject("GradeBadge");
            badge.transform.SetParent(parent, false);

            var badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 24;
            badgeLE.preferredHeight = 20;

            var borderImg = badge.AddComponent<Image>();

            var inner = new GameObject("Inner");
            inner.transform.SetParent(badge.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1, 1);
            innerRect.offsetMax = new Vector2(-1, -1);
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = BadgeBg;

            var label = new GameObject("Label");
            label.transform.SetParent(badge.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelTmp = CreateTMPText(label, letter, 11, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);

            if (gradeIdx >= 4) // S
            {
                borderImg.color = GoldColor;
                innerImg.color = GoldColor;
                labelTmp.color = Color.black;
            }
            else if (gradeIdx >= 3) // A
            {
                borderImg.color = GoldColor;
                innerImg.color = BadgeBg;
                labelTmp.color = GoldColor;
            }
            else // B, C, D
            {
                borderImg.color = BorderWhite;
                innerImg.color = BadgeBg;
                labelTmp.color = Color.white;
            }
        }

        public static Canvas CreatePanelCanvas(string name)
        {
            var root = new GameObject(name);
            Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static GameObject CreatePanelObject(Transform parent, Vector2 position)
        {
            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(parent, false);
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var img = panelObj.AddComponent<Image>();
            if (BgSprite != null)
            {
                img.sprite = BgSprite;
                img.type = (BgSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                img.color = PureBg;
            }
            else
            {
                img.color = PureBg;
            }

            return panelObj;
        }

        public static GameObject CreateHeader(Transform parent, string title)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, HeaderHeight);
            header.AddComponent<Image>().color = Color.clear;

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            CreateTMPText(titleObj, title, 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

            return header;
        }

        public static GameObject CreateContentParent(Transform parent)
        {
            var content = new GameObject("Content");
            content.transform.SetParent(parent, false);
            var rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(0, BottomPadding);
            rect.offsetMax = new Vector2(0, -HeaderHeight);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 1;

            return content;
        }
    }
}
