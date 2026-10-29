using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Qianhai.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(.8f, 1f)] private float pressedScale = .94f;
        [SerializeField, Min(.05f)] private float reboundDuration = .2f;
        [SerializeField] private Color pressedTint = new Color(.72f, .84f, 1f, 1f);
        private UnityEngine.UI.Button button;
        private Vector3 restingScale;
        private Color restingColor;
        private Coroutine animation;

        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            restingScale = transform.localScale;
            restingColor = button.targetGraphic != null ? button.targetGraphic.color : Color.white;
            button.onClick.AddListener(PlayFeedback);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && button.IsInteractable()) Press();
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Release();
        }

        public void OnPointerExit(PointerEventData data) => Release();

        public void PlayFeedback()
        {
            if (!isActiveAndEnabled || !button.IsInteractable()) return;
            Press();
            Release();
        }

        public void SetRestingColor(Color color)
        {
            restingColor = color;
            if (animation == null && button != null && button.targetGraphic != null)
                button.targetGraphic.color = color;
        }

        private void Press()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            transform.localScale = restingScale * pressedScale;
            if (button.targetGraphic != null) button.targetGraphic.color = pressedTint;
        }

        private void Release()
        {
            if (!isActiveAndEnabled) return;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(Rebound());
        }

        private IEnumerator Rebound()
        {
            var startScale = transform.localScale;
            var startColor = button.targetGraphic != null ? button.targetGraphic.color : restingColor;
            float elapsed = 0f;
            while (elapsed < reboundDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / reboundDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, restingScale, eased);
                if (button.targetGraphic != null)
                    button.targetGraphic.color = Color.Lerp(startColor, restingColor, eased);
                yield return null;
            }
            Restore();
            animation = null;
        }

        private void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (button != null) Restore();
        }

        private void Restore()
        {
            transform.localScale = restingScale;
            if (button.targetGraphic != null) button.targetGraphic.color = restingColor;
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(PlayFeedback);
        }
    }
}
