
using System.Collections.Generic;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Quản lý Time.timeScale bằng các slot. Time.timeScale = tích của tất cả timeScale trong slot.
    /// </summary>
    public class TimeScaleController : MonoBehaviour
    {
        [Tooltip("Các slot đang tác động lên time scale. Mỗi slot nhân thêm hệ số của nó vào Time.timeScale.")]
        [SerializeField] List<SlotTimeScale> slotTimeScales = new List<SlotTimeScale>();


        #region Singleton
        private static TimeScaleController ins;
        /// <summary>
        /// Instance duy nhất trong scene. Tự tìm trong scene nếu chưa có.
        /// </summary>
        public static TimeScaleController Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = FindObjectOfType<TimeScaleController>();
                    a?.Awake();
                }
                return ins;
            }
            set => ins = value;
        }
        #endregion

        private void Awake()
        {
            #region Singleton
            if (ins == null)
                ins = this;
            else
            {
                if (ins != this)
                    Destroy(gameObject);
                return;
            }
            #endregion


        }

        void OnDestroy()
        {
            // Chỉ reset khi instance đang hoạt động bị hủy, không reset khi là bản trùng bị Destroy
            if (ins == this)
                Time.timeScale = 1;
        }

        /// <summary>
        /// Tạo slot mới với hệ số timeScale và thêm vào danh sách.
        /// </summary>
        /// <param name="source">Component tạo slot, dùng để tra cứu/ghi chú</param>
        /// <param name="timeScale">Hệ số nhân vào Time.timeScale</param>
        public SlotTimeScale CreatSlot(Behaviour source, float timeScale)
        {
            var newSlot = new SlotTimeScale(source, timeScale);
            AddSlot(newSlot);
            return newSlot;
        }

        /// <summary>
        /// Thêm slot có sẵn vào danh sách (bỏ qua nếu đã có).
        /// </summary>
        public void AddSlot(SlotTimeScale timeScale)
        {
            if (!slotTimeScales.Contains(timeScale))
                slotTimeScales.Add(timeScale);
        }

        /// <summary>
        /// Gỡ slot khỏi danh sách, time scale sẽ không còn chịu ảnh hưởng của slot này.
        /// </summary>
        public void RemoveSlot(SlotTimeScale timeScale)
        {
            if (slotTimeScales.Contains(timeScale))
                slotTimeScales.Remove(timeScale);
        }

        private void Update()
        {
            float t = 1;
            foreach (var item in slotTimeScales)
            {
                t *= item.timeScale;
            }
            Time.timeScale = t;
        }

        /// <summary>
        /// Một slot tác động time scale.
        /// </summary>
        [System.Serializable]
        public class SlotTimeScale
        {
            [Tooltip("Ghi chú để debug, không ảnh hưởng logic.")]
            public string Description;
            [Tooltip("Component sở hữu slot này.")]
            public Behaviour source;
            [Tooltip("Hệ số nhân vào Time.timeScale (1 = không đổi, 0.5 = chậm một nửa).")]
            public float timeScale;

            public SlotTimeScale(Behaviour source, float timeScale)
            {
                this.source = source;
                this.timeScale = timeScale;
            }
        }
    }
}
