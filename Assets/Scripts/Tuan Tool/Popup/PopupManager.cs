using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TuanTool.Popup
{
    /// <summary>
    /// Quản lý hàng đợi popup: mỗi lần chỉ hiện popup đầu hàng, popup sau được hiện khi popup trước bị gỡ.
    /// Tự tạo instance nếu scene chưa có.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [Tooltip("Popup đang hiển thị (chỉ đọc, do PopupManager cập nhật).")]
        public Popup Currentpopup;

        [Tooltip("Hàng đợi popup, phần tử đầu tiên là popup sẽ được hiển thị.")]
        public List<Popup> popups = new List<Popup>();

        #region Singleton
        private static PopupManager ins;

        /// <summary>
        /// Instance dùng chung. Nếu scene chưa có PopupManager sẽ tự tạo một GameObject mới.
        /// </summary>
        public static PopupManager Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = FindObjectOfType<PopupManager>();
                    a?.Awake();
                    if (a == null)
                    {
                        Debug.Log("Creat new PopupManager!!!");
                        GameObject popup = new GameObject(nameof(PopupManager));
                        popup.AddComponent<PopupManager>().Awake();
                    }
                }
                return ins;
            }
            set => ins = value;
        }
        #endregion

        private void Awake()
        {
            if (ins == null)
                ins = this;
            else
            {
                if (ins != this)
                    Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Hiện popup đầu hàng nếu nó chưa hiện. Nếu hàng rỗng thì Currentpopup = null.
        /// Popup đã bị destroy sẽ bị loại khỏi hàng trước để không chặn hàng đợi.
        /// </summary>
        public void Process()
        {
            popups.RemoveAll(p => p == null);

            if (popups.Count > 0)
            {
                if (!popups[0].IsShowing)
                {
                    popups[0].Show(null);
                    Currentpopup = popups[0];
                }
            }
            else
                Currentpopup = null;
        }

        /// <summary>
        /// Thêm popup vào cuối hàng đợi (bỏ qua nếu đã có) rồi thử hiện.
        /// </summary>
        public void AddPopup(Popup popup)
        {
            if (!popups.Contains(popup))
            {
                popups.Add(popup);
                Process();
            }
        }

        /// <summary>
        /// Gỡ popup khỏi hàng đợi rồi thử hiện popup kế tiếp.
        /// </summary>
        public void RemovePopup(Popup popup)
        {
            if (popups.Contains(popup))
            {
                popups.Remove(popup);
                Process();
            }
        }
    }
}
