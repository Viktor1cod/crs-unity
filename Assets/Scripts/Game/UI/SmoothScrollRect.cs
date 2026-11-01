using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>
    /// Vertical ScrollRect with smooth, limited mouse-wheel and trackpad scrolling.
    /// Browsers report very large wheel deltas, so each event is clamped
    /// and the content glides to the target instead of jumping.
    /// Touch dragging keeps the standard ScrollRect behaviour.
    /// </summary>
    public sealed class SmoothScrollRect : ScrollRect
    {
        /// <summary>Distance in canvas units for one wheel step.</summary>
        public float WheelStep = 70f;
        /// <summary>Upper limit of steps taken from a single wheel event.</summary>
        public float MaxStepsPerEvent = 2f;
        /// <summary>Time to reach the target, in seconds.</summary>
        public float SmoothTime = 0.12f;

        private float targetY;
        private float velocityY;
        private bool wheelScrolling;

        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive() || content == null) return;

            float delta = Mathf.Clamp(data.scrollDelta.y, -MaxStepsPerEvent, MaxStepsPerEvent);
            if (Mathf.Approximately(delta, 0f)) return;

            if (!wheelScrolling)
            {
                targetY = content.anchoredPosition.y;
                velocityY = 0f;
                wheelScrolling = true;
            }

            StopMovement();
            targetY = Mathf.Clamp(targetY - delta * WheelStep, 0f, MaxOffset());
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            wheelScrolling = false;
            base.OnBeginDrag(eventData);
        }

        /// <summary>Jump to the top of the list without animation.</summary>
        public void ScrollToTop()
        {
            wheelScrolling = false;
            StopMovement();
            if (content != null) content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!wheelScrolling || content == null) return;

            var position = content.anchoredPosition;
            targetY = Mathf.Clamp(targetY, 0f, MaxOffset());
            float y = Mathf.SmoothDamp(position.y, targetY, ref velocityY, SmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(y - targetY) < 0.5f)
            {
                y = targetY;
                wheelScrolling = false;
            }
            SetContentAnchoredPosition(new Vector2(position.x, y));
        }

        private float MaxOffset()
        {
            var view = viewport != null ? viewport : (RectTransform)transform;
            return Mathf.Max(0f, content.rect.height - view.rect.height);
        }
    }
}
