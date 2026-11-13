using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TuanTool
{
    [DisallowMultipleComponent]
    [Tooltip("Tự động cuộn ScrollRect để hiện item được chọn trong Dropdown hoặc Menu")]
    public class DropdownAutoScroll : MonoBehaviour
    {
        [Tooltip("ScrollRect chứa các item cần cuộn")]
        public ScrollRect scrollRect; // Gán ScrollRect chứa các item
        [Tooltip("Container (Content) chứa các item. Chỉ cuộn khi item đang chọn nằm trong container này")]
        public RectTransform container; // container chua item dang chon

        void Update()
        {
            // EventSystem có thể chưa tồn tại trong scene (ví dụ scene test): tránh NullReferenceException
            if (EventSystem.current == null) return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != null && selected.transform.IsChildOf(container.transform))
            {
                RectTransform selectedRect = selected.GetComponent<RectTransform>();
                ScrollToSelected(selectedRect);
            }
        }

        /// <summary>
        /// Đặt verticalNormalizedPosition của ScrollRect để target nằm trong khung nhìn.
        /// </summary>
        void ScrollToSelected(RectTransform target)
        {

            var normalized = TMath.Get_Normalized_RectA_In_RectB(target, container);
            scrollRect.verticalNormalizedPosition = normalized.y;


        }
    }
}