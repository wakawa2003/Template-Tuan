using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TuanTool
{
    [AddComponentMenu("Layout/Scale Rect With Parent")]
    [ExecuteInEditMode, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public class ScaleRectWithParent : MonoBehaviour
    {
        public float LeftNormal = 0;
        public float RightNormal = 0;
        public float TopNormal = 0;
        public float BotNormal = 0;

        RectTransform RectTransform, parentRect;
        [ReadOnly]
        [SerializeField] Vector2 parentSize;
        Vector2 newOffsetMin, newOffsetMax;

        private void Start()
        {
            RectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent.GetComponent<RectTransform>();
         
            ExecuteScale();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            //Debug.Log($"reset");
            GetCurrentNormal();
            ExecuteScale();
        }
#endif

        void LateUpdate()
        {
            ExecuteScale();
        }

        [Button]
        public void GetCurrentNormal()
        {
            RectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentSize != parentRect.rect.size)
                parentSize = parentRect.rect.size;
            LeftNormal = TMath.Remap(RectTransform.offsetMin.x, 0, parentSize.x, 0, 1);
            RightNormal = TMath.Remap(RectTransform.offsetMax.x, 0, parentSize.x, 0, 1);
            BotNormal = TMath.Remap(RectTransform.offsetMin.y, 0, parentSize.y, 0, 1);
            TopNormal = TMath.Remap(RectTransform.offsetMax.y, 0, parentSize.y, 0, 1);
        }

        [Button]
        public void ExecuteScale()
        {

            if (parentSize != parentRect.rect.size)
                parentSize = parentRect.rect.size;
            var newLeft = TMath.Remap(LeftNormal, 0, 1, 0, parentSize.x);
            var newRight = TMath.Remap(RightNormal, 0, 1, 0, parentSize.x);
            var newTop = TMath.Remap(TopNormal, 0, 1, 0, parentSize.y);
            var newBot = TMath.Remap(BotNormal, 0, 1, 0, parentSize.y);

            newOffsetMin = new Vector2(newLeft, newBot);
            newOffsetMax = new Vector2(newRight, newTop);

            if (RectTransform.offsetMin != newOffsetMin)
                RectTransform.offsetMin = newOffsetMin;
            if (RectTransform.offsetMax != newOffsetMax)
                RectTransform.offsetMax = newOffsetMax;

        }


    }
}
