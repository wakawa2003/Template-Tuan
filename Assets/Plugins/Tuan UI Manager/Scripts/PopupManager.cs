using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TuanTool.Popup
{
    public class PopupManager : MonoBehaviour, IPopupManager
    {
        [SerializeField] private List<RequestData> popupQueue = new List<RequestData>();


        #region Singleton
        private static PopupManager ins;
        public static PopupManager Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = new GameObject("PopupManager").AddComponent<PopupManager>();
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
            DontDestroyOnLoad(gameObject);

            UniTask.Create(async () =>
            {
                while (true)
                {
                    if (popupQueue.Count > 0)
                    {
                        var popup = popupQueue[0];
                        try
                        {
                            await popup.Popup.Show(null);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                        popupQueue.RemoveAt(0);
                    }

                }
            }).Forget();
        }

        public void AddPopup(IPopup popup, object payloadData)
        {
            RequestData requestData = new RequestData(GetNewID(), payloadData, popup);
            popupQueue.Add(requestData);
        }

        string GetNewID()
        {
            return System.Guid.NewGuid().ToString();
        }

        [System.Serializable]
        public class RequestData
        {
            public RequestData(string id, object payloadData, IPopup popup)
            {
                this.id = id;
                this.payloadData = payloadData;
                this.popup = popup;
            }

            private string id;
            private object payloadData;
            private IPopup popup;

            public string ID { get => id; private set => id = value; }
            public object PayloadData { get => payloadData; private set => payloadData = value; }
            public IPopup Popup { get => popup; private set => popup = value; }

            public async Task<UniTask> Show()
            {
                try
                {
                    // Show popup logic
                    await popup.Show(payloadData);
                    await popup.Hide();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
