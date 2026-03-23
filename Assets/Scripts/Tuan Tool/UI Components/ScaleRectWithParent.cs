using Sirenix.OdinInspector;
using Unity.Burst.Intrinsics;
using UnityEngine;

namespace TuanTool
{
    [AddComponentMenu("Layout/Scale Rect With Target")]
    [ExecuteInEditMode, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public class ScaleRectWithTarget : MonoBehaviour
    {

        [SerializeField] private Vector2 offsetMin = Vector2.zero;
        [SerializeField] private Vector2 offsetMax = Vector2.zero;

        [SerializeField] private RectTransform RectTransform;
        [SerializeField] private EGetTargetType getTargetType = EGetTargetType.SetManual;
        [SerializeField, EnableIf(nameof(getTargetType), EGetTargetType.SetManual)] private RectTransform targetRect; // target cụ thể, gán trong Inspector
        [SerializeField] private bool isFitSizeOnly = true;


        public enum EGetTargetType
        {
            SetManual, GetParentCanvas, GetParentRectTransform
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
            if (rectA == null || rectB == null) return;

            // Lấy world corners của B
            Vector3[] worldCorners = new Vector3[4];
            rectB.GetWorldCorners(worldCorners);


            // Chuyển world corners về local space của cha A
            Transform parent = rectA.parent;
            Vector2 min = (Vector2)parent.InverseTransformPoint(worldCorners[0]) + offsetMin;
            Vector2 max = (Vector2)parent.InverseTransformPoint(worldCorners[2]) + offsetMax;

            // Gán lại anchoredPosition và sizeDelta cho A
            rectA.anchorMin = rectA.anchorMax = Vector2.zero; // dùng pivot cố định
            rectA.offsetMin = min;
            rectA.offsetMax = max;
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

            rectA.anchorMin = rectA.anchorMax = Vector2.one / 2f; ; // dùng pivot cố định
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
                    targetRect = GetComponentInParent<Canvas>().transform as RectTransform;
                    break;
                case EGetTargetType.GetParentRectTransform:
                    targetRect = transform.parent as RectTransform;
                    break;
            }

        }

        [Button]
        public void ExecuteScale()
        {
            // if (targetRect == null) return;

            // targetSize = targetRect.rect.size;

            // var newLeft = TMath.Remap(LeftNormal, 0, 1, 0, targetSize.x);
            // var newRight = TMath.Remap(RightNormal, 0, 1, 0, targetSize.x);
            // var newTop = TMath.Remap(TopNormal, 0, 1, 0, targetSize.y);
            // var newBot = TMath.Remap(BotNormal, 0, 1, 0, targetSize.y);

            // newOffsetMin = new Vector2(newLeft, newBot);
            // newOffsetMax = new Vector2(newRight, newTop);

            // if (RectTransform.offsetMin != newOffsetMin)
            //     RectTransform.offsetMin = newOffsetMin;
            // if (RectTransform.offsetMax != newOffsetMax)
            //     RectTransform.offsetMax = newOffsetMax;

            if (isFitSizeOnly)
                FitSizeOnly(RectTransform, targetRect);
            else
                FitToTarget(RectTransform, targetRect);
        }
    }
}