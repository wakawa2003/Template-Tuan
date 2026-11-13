using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TuanTool.Processing
{
    /// <summary>
    /// Quản lý các tiến trình đang chạy (loading, lưu dữ liệu...). Khi còn ít nhất một tiến trình ở trạng thái Processing,
    /// ProcessingPopup sẽ hiện lên. Tồn tại xuyên scene (DontDestroyOnLoad).
    /// </summary>
    public class ProcessingController : MonoBehaviour
    {
        #region Singleton
        private static ProcessingController ins;

        /// <summary>
        /// Instance dùng chung. Nếu scene chưa có sẽ tự tạo GameObject "Processing Controller".
        /// </summary>
        public static ProcessingController Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = FindObjectOfType<ProcessingController>() ?? new GameObject("Processing Controller").AddComponent<ProcessingController>();
                    a?.Awake();
                }
                return ins;
            }
            set => ins = value;
        }

        #endregion
        [Tooltip("Danh sách tiến trình đang theo dõi. Tự xóa khi tiến trình không còn ở trạng thái Processing.")]
        [SerializeField] List<ProcessStack> processStacks = new List<ProcessStack>();

        /// <summary>
        /// Thêm tiến trình vào danh sách theo dõi (bỏ qua nếu đã có).
        /// </summary>
        public void AddProcess(ProcessStack processStack)
        {
            if (!ProcessStacks.Contains(processStack))
            {
                ProcessStacks.Add(processStack);
            }
        }

        /// <summary>
        /// Gỡ tiến trình khỏi danh sách theo dõi.
        /// </summary>
        public void RemoveProcess(ProcessStack processStack)
        {
            if (ProcessStacks.Contains(processStack))
            {
                ProcessStacks.Remove(processStack);
            }
        }

        /// <summary>
        /// Tạo một tiến trình mới với tên <paramref name="name"/>, thêm vào danh sách và trả về để caller cập nhật trạng thái.
        /// </summary>
        public ProcessStack CreatProcess(string name)
        {
            var newProcess = new ProcessStack(name);
            AddProcess(newProcess);
            return newProcess;
        }

        /// <summary>
        /// Danh sách tiến trình đang theo dõi (chỉ đọc từ bên ngoài).
        /// </summary>
        public List<ProcessStack> ProcessStacks { get => processStacks; private set => processStacks = value; }

        void Awake()
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
            DontDestroyOnLoad(gameObject);


        }

        void Update()
        {
            foreach (var item in ProcessStacks.ToArray())
            {
                if (item.State != ProcessStack.EState.Processing)
                {
                    RemoveProcess(item);
                }
            }
        }


        /// <summary>
        /// Một tiến trình cần theo dõi. Gọi SetCompletedState hoặc SetErrorState khi xong để ProcessingController gỡ nó ra.
        /// </summary>
        [System.Serializable]
        public class ProcessStack
        {

            [SerializeField] private string name;

            /// <summary>
            /// Tạo tiến trình với tên hiển thị/debug và trạng thái ban đầu là Processing.
            /// </summary>
            public ProcessStack(string name)
            {
                this.name = name;
            }

            /// <summary>
            /// none: chưa bắt đầu; Processing: đang chạy; Completed: xong; Error: lỗi (xem Exception).
            /// </summary>
            public enum EState { none, Processing, Completed, Error }
            private System.Exception exception;
            [SerializeField] private EState state = EState.Processing;

            public EState State { get => state; private set => state = value; }
            /// <summary>
            /// Exception được ghi nhận khi SetErrorState, null nếu không lỗi.
            /// </summary>
            public Exception Exception { get => exception; private set => exception = value; }
            public string Name { get => name; }

            /// <summary>
            /// Đánh dấu tiến trình đã hoàn thành thành công.
            /// </summary>
            public void SetCompletedState()
            {
                State = EState.Completed;
            }

            /// <summary>
            /// Đánh dấu tiến trình bị lỗi và lưu exception để debug.
            /// </summary>
            public void SetErrorState(System.Exception Exception)
            {
                State = EState.Error;
                this.Exception = Exception;
            }
        }
    }
}
