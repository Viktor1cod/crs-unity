using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>Small helpers to build uGUI from code without prefabs.</summary>
    internal static class Ui
    {
        /// <summary>Bundled font with Cyrillic: Assets/Resources/Fonts/Main.ttf.</summary>
        private const string FontResource = "Fonts/Main";

        public static Font Font { get; private set; }

        public static void LoadFont()
        {
            if (Font != null) return;
            Font = Resources.Load<Font>(FontResource);
            if (Font == null)
            {
                Debug.LogWarning($"Ui: font '{FontResource}' not found, Cyrillic will not render in Web builds.");
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, int size, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, int fontSize, Color background,
            UnityAction onClick, out Text label)
        {
            var image = CreateImage(name, parent, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var layout = image.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 12, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            label = CreateText("Label", image.transform, fontSize, Theme.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }

        /// <summary>Stretch to fill the parent with optional margins.</summary>
        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Layout sizes; -1 means "not set".</summary>
        public static LayoutElement Layout(Component target,
            float minWidth = -1, float preferredWidth = -1, float flexibleWidth = -1,
            float minHeight = -1, float preferredHeight = -1, float flexibleHeight = -1)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.minWidth = minWidth;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = minHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        /// <summary>Column: children take full width and their preferred height.</summary>
        public static VerticalLayoutGroup Vertical(GameObject go, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Row: children take their preferred width; flexible ones share the rest.</summary>
        public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
    }
}
