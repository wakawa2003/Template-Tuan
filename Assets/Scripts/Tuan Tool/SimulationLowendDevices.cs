using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Chỉ dùng trong Editor: chạy vòng lặp tính toán nặng mỗi frame để giả lập máy cấu hình thấp.
    /// Dùng để kiểm tra FPS/hiệu năng. Không đưa vào build.
    /// </summary>
    public class SimulationLowendDevices : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("Số vòng lặp tính toán mỗi lần xử lý. Tăng lên để giả lập máy yếu hơn.")]
        public int countProcess = 1000;
        [Tooltip("Hệ số ngẫu nhiên nhân với countProcess (x = min, y = max) để tải không đều mỗi frame.")]
        public Vector2 rangeRandom = new Vector2(0.8f, 1.2f);
        // Update is called once per frame
        void Update()
        {
            PeriodProcess();
        }


        private void FixedUpdate()
        {
            PeriodProcess();
        }
        private void LateUpdate()
        {
            PeriodProcess();
        }
        void SingleProcess()
        {
            for (int i = 0; i < 100; i++)
            {
                // Kết quả không dùng, chỉ để tạo tải CPU
                var a = Mathf.Pow(5000, 3000);
            }
        }

        private void PeriodProcess()
        {
            var n = Random.Range(countProcess * rangeRandom.x, countProcess * rangeRandom.y);
            for (int i = 0; i < n; i++)
            {
                SingleProcess();
            }
        }

#endif
    }
}
