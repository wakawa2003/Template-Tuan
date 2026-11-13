using DG.Tweening;
using DG.Tweening.Core.Easing;
using System;
using UnityEngine;

namespace TuanTool.Processing
{
    /// <summary>
    /// Popup loading xoay icon, tự hiện khi ProcessingController có tiến trình đang chạy và tự ẩn khi xong.
    /// Tồn tại xuyên scene (DontDestroyOnLoad).
    /// </summary>
    public class ProcessingPopup : Popup.Popup
    {
        private Tween rotateTween;

        [Header("Settings")]
        [Tooltip("Thời gian (giây) để icon xoay đủ một vòng 360 độ.")]
        [SerializeField] private float rotationSpeed = 1f;
        [Tooltip("Transform của icon cần xoay (thường là icon loading con của content).")]
        [SerializeField] private Transform iconTransform;

        #region Singleton
        private static ProcessingPopup ins;

        /// <summary>
        /// Instance dùng chung, tìm trong scene nếu chưa có.
        /// </summary>
        public static ProcessingPopup Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = FindObjectOfType<ProcessingPopup>();
                    a?.Awake();
                }
                return ins;
            }
            set => ins = value;
        }
        #endregion

        protected override void Awake()
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
            base.Awake();

            DontDestroyOnLoad(gameObject);
        }


        protected override void Update()
        {
            base.Update();
            var process = ProcessingController.Ins;

            bool isProcessing = false;
            foreach (var item in process.ProcessStacks.ToArray())
            {
                if (item.State == ProcessingController.ProcessStack.EState.Processing)
                    isProcessing = true;
            }

            if (IsShowing && !isProcessing)
                Hide();
            else if (!IsShowing && isProcessing)
                RequestShow();

        }

        /// <summary>
        /// Hiện popup và bắt đầu xoay icon (vòng lặp vô hạn, không phụ thuộc timeScale).
        /// </summary>
        public override void Show(Action onComplete = null)
        {
            base.Show(onComplete);
            rotateTween?.Kill();
            rotateTween = iconTransform.DORotate(new Vector3(0, 0, -360), rotationSpeed, RotateMode.FastBeyond360)
       .SetEase(Ease.Linear)
       .SetLoops(-1, LoopType.Restart)
       .SetLink(Ins.gameObject)
       .SetUpdate(true)
       .OnKill(() => rotateTween = null);
        }
    }
}