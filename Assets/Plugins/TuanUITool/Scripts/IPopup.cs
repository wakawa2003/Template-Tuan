using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace UITool.Popup
{
    public interface IPopup
    {
        public static UnityEvent<IPopup, object> OnPopupRequestShow = new UnityEvent<IPopup, object>();

        /// <summary>
        ///When Start Open Popup
        /// </summary>
        public static UnityEvent<IPopup> OnPopupStartOpen = new UnityEvent<IPopup>();

        /// <summary>
        /// When popup was opened
        /// </summary>
        public static UnityEvent<IPopup> OnPopupOpened = new UnityEvent<IPopup>();

        /// <summary>
        /// When Start Close Popup
        /// </summary>
        public static UnityEvent<IPopup> OnPopupStartClose = new UnityEvent<IPopup>();

        /// <summary>
        /// When popup was closed
        /// </summary>
        public static UnityEvent<IPopup> OnPopupClosed = new UnityEvent<IPopup>();

        /// <summary>
        /// Show popup use queue in Popup Manager, Dont chnage this value runtime
        /// </summary>
        public bool IsUseQueue { get; }
        public bool IsShowing { get; }

        public IPopupManager Manager => UITool.Popup.PopupManager.Ins;

        /// <summary>
        /// return popup id when request show
        /// </summary>
        /// <param name="payloadData"></param>
        /// <returns></returns>
        public string RequestShow(object payloadData)
        {
            OnPopupRequestShow?.Invoke(this, payloadData);
            if (IsUseQueue)
                return Manager.AddShowPopupRequest(this, payloadData);
            else
                Show(payloadData);
            return null;
        }

        public UniTask Show(object payloadData);
        public UniTask Hide();
    }
}
