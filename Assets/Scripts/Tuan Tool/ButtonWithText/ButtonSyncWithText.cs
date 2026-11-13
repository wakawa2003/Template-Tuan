
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TuanTool
{
    /// <summary>
    /// Button đồng bộ màu với text: khi đổi trạng thái (Normal/Highlighted/Pressed/...),
    /// CanvasGroup chứa text sẽ fade theo độ sáng của màu tương ứng trong ColorBlock.
    /// Yêu cầu Transition = ColorTint.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ButtonSyncWithText : Button
    {
        [Tooltip("CanvasGroup của text cần fade theo trạng thái button. Tự tìm trong con nếu để trống.")]
        public CanvasGroup m_CanvasGroup;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Color tintColor;
            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = colors.normalColor;
                    break;
                case SelectionState.Highlighted:
                    tintColor = colors.highlightedColor;
                    break;
                case SelectionState.Pressed:
                    tintColor = colors.pressedColor;
                    break;
                case SelectionState.Selected:
                    tintColor = colors.selectedColor;
                    break;
                case SelectionState.Disabled:
                    tintColor = colors.disabledColor;
                    break;
                default:
                    tintColor = Color.black;
                    break;
            }
            if (transition == Transition.ColorTint)
                if (m_CanvasGroup != null)
                {
                    float a = tintColor.a * tintColor.r * tintColor.g * tintColor.b;
                    // SetLink: tween tự kill khi CanvasGroup bị destroy (thay cho TweenExtension cũ)
                    m_CanvasGroup.DOFade(a, instant ? 0f : colors.fadeDuration).SetUpdate(true).SetLink(m_CanvasGroup.gameObject);
                }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Cập nhật alpha của CanvasGroup ngay trong Inspector khi đổi màu/interactable.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponentInChildren<CanvasGroup>();
            if (m_CanvasGroup == null) return;

            Color tintColor = interactable ? colors.normalColor : colors.disabledColor;
            float a = tintColor.a * tintColor.r * tintColor.g * tintColor.b;
            m_CanvasGroup.alpha = a * colors.colorMultiplier;
        }

        /// <summary>
        /// Khi thêm component lần đầu, tự gán CanvasGroup nằm trong con.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            if (!m_CanvasGroup) m_CanvasGroup = GetComponentInChildren<CanvasGroup>();
        }
#endif
    }
}