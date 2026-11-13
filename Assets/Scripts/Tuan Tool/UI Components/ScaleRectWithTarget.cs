using Sirenix.OdinInspector;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Đồng bộ RectTransform của object này với một RectTransform đích (target):
    /// copy vị trí và/hoặc kích thước của target. Hữu ích cho lớp nền/viền bám theo panel cha.
    /// Tên file phải trùng tên class (ScaleRectWithTarget).
    /// </summary>
    [AddComponentMenu("Layout/Scale Rect With Target")]
    [ExecuteInEditMode, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(99999)]
    public class ScaleRectWithTarget : MonoBehaviour
    {

        [Tooltip("Offset thêm vào góc dưới-trái (min) sau khi fit")]
        [SerializeField] private Vector2 offsetMin = Vector2.zero;
        [Tooltip("Offset thêm vào góc trên-phải (max) sau khi fit")]
        [SerializeField] private Vector2 offsetMax = Vector2.zero;

        [SerializeField] private RectTransform RectTransform;
        [Tooltip("Cách lấy target: SetManual (gán tay), GetParentCanvas (Canvas cha), GetParentRectTransform (RectTransform cha)")]
        [SerializeField] private EGetTargetType getTargetType = EGetTargetType.SetManual;
        [SerializeField, EnableIf(nameof(getTargetType), EGetTargetType.SetManual)] private RectTransform targetRect; // target cụ thể, gán trong Inspector
        [Tooltip("Có copy vị trí của target không")]
        [SerializeField] private bool isSyncPosition = true;
        [Tooltip("Có copy kích thước của target không")]
        [SerializeField] private bool isFitSize = true;


        /// <summary>Cách xác định RectTransform đích.</summary>
        public enum EGetTargetType
        {
            /// <summary>Gán target thủ công trong Inspector.</summary>
            SetManual,
            /// <summary>Lấy transform của Canvas cha.</summary>
            GetParentCanvas,
            /// <summary>Lấy RectTransform của object cha trực tiếp.</summary>
            GetParentRectTransform
        }

        private void Start()
        {
            GetData();
            ExecuteScale();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            GetData();
            GetCurrentNormal();
            ExecuteScale();
        }
#endif
        /// <summary>
        /// Chỉnh RectTransform A fit với RectTransform B bất kể A đang là con của ai.
        /// </summary>
        public void FitToTarget(RectTransform rectA, RectTransform rectB)
        {
            rectA.transform.position = rectB.transform.position;
        }
        /// <summary>
        /// Chỉnh RectTransform A có cùng size với RectTransform B,
        /// nhưng giữ nguyên vị trí hiện tại của A.
        /// </summary>
        public void FitSizeOnly(RectTransform rectA, RectTransform rectB)
        {
            if (rectA == null || rectB == null) return;

            // Lấy world corners của B
            Vector3[] worldCorners = new Vector3[4];
            rectB.GetWorldCorners(worldCorners);

            // Chuyển về local space của cha A
            Transform parent = rectA.parent;
            Vector2 min = (Vector2)parent.InverseTransformPoint(worldCorners[0]) + offsetMin;
            Vector2 max = (Vector2)parent.InverseTransformPoint(worldCorners[2]) + offsetMax;

            // Tính size theo local space
            Vector2 size = max - min;

            rectA.anchorMin = rectA.anchorMax = Vector2.one / 2f; // dùng pivot cố định
            // Gán sizeDelta, giữ nguyên anchoredPosition
            rectA.sizeDelta = size;
        }

        private void LateUpdate()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                GetData();
            }
            ExecuteScale();
        }

        [Button]
        public void GetCurrentNormal()
        {
            GetData();
        }

        private void GetData()
        {
            if (RectTransform == null)
                RectTransform = GetComponent<RectTransform>();


            switch (getTargetType)
            {
                case EGetTargetType.GetParentCanvas:
                    var canvas = GetComponentInParent<Canvas>();
                    if (canvas == null)
                        Debug.LogWarning($"[ScaleRectWithTarget] Không tìm thấy Canvas cha của {name}", this);
                    targetRect = canvas != null ? canvas.transform as RectTransform : null;
                    break;
                case EGetTargetType.GetParentRectTransform:
                    targetRect = transform.parent as RectTransform;
                    break;
            }

        }

        /// <summary>
        /// Áp dụng fit size / sync position theo cấu hình hiện tại.
        /// </summary>
        [Button]
        public void ExecuteScale()
        {
            if (targetRect == null) return;
            if (isFitSize)
                FitSizeOnly(RectTransform, targetRect);
            if (isSyncPosition)
                FitToTarget(RectTransform, targetRect);
        }
    }
}