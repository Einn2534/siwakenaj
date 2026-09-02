using UnityEngine;
using UnityEngine.EventSystems;

namespace Siwakenja.Experiments.InputUIComparison
{
    /// <summary>
    /// Describes one logical experiment control. Physical pointer logging deliberately does
    /// not happen here: GlobalRawPointerCapture is the single source of raw input rows.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup))]
    public sealed class ExperimentPointerInput : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private ExperimentInput _input;
        private RectTransform _rect;
        private UnityEngine.UI.Image _image;
        private UnityEngine.UI.Text _label;
        private CanvasGroup _canvasGroup;
        private bool _isVisible;
        private bool _isInteractable;

        public ExperimentInput Input => _input;
        public string ButtonId => _input == ExperimentInput.None
            ? string.Empty
            : _input.ToString().ToLowerInvariant();
        public bool IsVisible => _isVisible;
        public bool IsInteractable => _isInteractable;
        public bool BlocksRaycasts => _canvasGroup != null && _canvasGroup.blocksRaycasts;

        // Compatibility alias for legacy engineering previews.
        public bool VisuallyEnabled => _isVisible && _isInteractable;

        public void Configure(InputUIComparisonController controller, ExperimentInput input, string label)
        {
            _input = input;
            CacheComponents();
            if (_label != null) _label.text = label;
            SetPresentation(true, true);
        }

        public void SetPresentation(bool visible, bool interactable)
        {
            CacheComponents();
            _isVisible = visible;
            _isInteractable = visible && interactable;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = _isInteractable;
            _canvasGroup.blocksRaycasts = visible;
            _image.color = _isInteractable ? ExperimentUIFactory.Control : ExperimentUIFactory.Disabled;
            if (_label != null)
            {
                _label.color = _isInteractable
                    ? ExperimentUIFactory.Ink
                    : new Color32(120, 124, 130, 255);
            }
        }

        public void SetVisualEnabled(bool enabled)
        {
            SetPresentation(true, enabled);
        }

        public Vector2 ScreenCenter()
        {
            CacheComponents();
            return RectTransformUtility.WorldToScreenPoint(null, _rect.TransformPoint(_rect.rect.center));
        }

        public Rect ScreenRect()
        {
            CacheComponents();
            var corners = new Vector3[4];
            _rect.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            CacheComponents();
            return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPoint, null);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isInteractable)
            {
                transform.localScale = new Vector3(0.96f, 0.96f, 1f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        private void CacheComponents()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_image == null) _image = GetComponent<UnityEngine.UI.Image>();
            if (_label == null) _label = GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
