
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TuanTool
{
    public class ButtonSyncWithText : Button
    {
        public TextMeshProUGUI m_Text;
        public Color m_colorText = Color.white;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Color tintColor;
            m_Text.color = Color.white * m_colorText;
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
                m_Text.CrossFadeColor(m_colorText * tintColor * colors.colorMultiplier, instant ? 0f : colors.fadeDuration, true, true);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Text.color = Color.white * m_colorText;
            if (interactable)
                m_Text.color = m_colorText * colors.normalColor * colors.colorMultiplier;
            else m_Text.color = m_colorText * colors.disabledColor;
        }

        protected override void Reset()
        {
            base.Reset();
            if (!m_Text) m_Text = GetComponentInChildren<TextMeshProUGUI>();
        }
#endif
    }
}