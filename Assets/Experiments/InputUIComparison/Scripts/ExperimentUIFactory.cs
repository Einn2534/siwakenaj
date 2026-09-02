using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Siwakenja.Experiments.InputUIComparison
{
    internal static class ExperimentUIFactory
    {
        private static Font _font;

        public static readonly Color CanvasBackground = new Color32(238, 240, 242, 255);
        public static readonly Color Panel = new Color32(255, 255, 255, 245);
        public static readonly Color Ink = new Color32(31, 35, 40, 255);
        public static readonly Color MutedInk = new Color32(88, 96, 105, 255);
        public static readonly Color Control = new Color32(220, 224, 229, 255);
        public static readonly Color ControlPressed = new Color32(194, 201, 209, 255);
        public static readonly Color Disabled = new Color32(225, 227, 230, 150);
        public static readonly Color Accent = new Color32(54, 102, 138, 255);
        public static readonly Color Correct = new Color32(35, 120, 76, 255);
        public static readonly Color Error = new Color32(176, 56, 50, 255);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static RectTransform Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            Color? color = null)
        {
            RectTransform rect = Rect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.alignment = alignment;
            text.color = color ?? Ink;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(
            string name,
            Transform parent,
            string label,
            int fontSize,
            UnityAction onClick)
        {
            Image background = Image(name, parent, Control);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Control;
            colors.highlightedColor = new Color32(231, 234, 238, 255);
            colors.pressedColor = ControlPressed;
            colors.selectedColor = Control;
            colors.disabledColor = Disabled;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            Text text = Text("Label", background.transform, label, fontSize);
            Stretch(text.rectTransform, 12f, 8f);
            return button;
        }

        public static UnityEngine.UI.InputField InputField(
            string name,
            Transform parent,
            string placeholder,
            int fontSize,
            UnityEngine.UI.InputField.ContentType contentType = UnityEngine.UI.InputField.ContentType.Standard)
        {
            Image background = Image(name, parent, Color.white);
            background.gameObject.AddComponent<Outline>().effectColor = new Color32(170, 176, 184, 255);
            UnityEngine.UI.InputField field = background.gameObject.AddComponent<UnityEngine.UI.InputField>();
            field.contentType = contentType;
            field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;

            Text placeholderText = Text("Placeholder", background.transform, placeholder, fontSize, TextAnchor.MiddleLeft, new Color32(140, 145, 152, 255));
            Stretch(placeholderText.rectTransform, 18f, 10f);
            Text valueText = Text("Text", background.transform, string.Empty, fontSize, TextAnchor.MiddleLeft);
            Stretch(valueText.rectTransform, 18f, 10f);
            field.placeholder = placeholderText;
            field.textComponent = valueText;
            return field;
        }

        public static void Stretch(RectTransform rect, float horizontal = 0f, float vertical = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("Fonts & Materials/DotGothic16-Regular");
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }
    }

    internal sealed class ExperimentRatingControl
    {
        private readonly Button[] _buttons;
        private readonly Image[] _images;

        public ExperimentRatingControl(Button[] buttons)
        {
            _buttons = buttons;
            _images = new Image[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                _images[i] = buttons[i].GetComponent<Image>();
            }
        }

        public int Value { get; private set; }

        public void Select(int value)
        {
            Value = value;
            for (int i = 0; i < _buttons.Length; i++)
            {
                _images[i].color = i + 1 == value ? ExperimentUIFactory.Accent : ExperimentUIFactory.Control;
                Text label = _buttons[i].GetComponentInChildren<Text>();
                label.color = i + 1 == value ? Color.white : ExperimentUIFactory.Ink;
            }
        }
    }
}
